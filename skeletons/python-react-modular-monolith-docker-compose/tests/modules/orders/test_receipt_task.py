import uuid

from httpx import AsyncClient

from app.modules.orders.tasks import generate_order_receipt


async def _login(client: AsyncClient, email: str, password: str) -> dict[str, str]:
    response = await client.post("/api/auth/login", json={"email": email, "password": password})
    assert response.status_code == 200
    return {"Authorization": f"Bearer {response.json()['data']['accessToken']}"}


async def test_receipt_request_returns_202_with_task_id(client: AsyncClient, users: dict) -> None:
    admin = await _login(client, "admin@example.com", "Admin123!")
    agent = await _login(client, "agent@example.com", "Agent123!")
    product = await client.post(
        "/api/products", json={"sku": "SKU-RCPT", "name": "Widget"}, headers=admin
    )
    order = await client.post(
        "/api/orders",
        json={"productId": product.json()["data"]["id"], "quantity": 1},
        headers=agent,
    )
    order_id = order.json()["data"]["id"]

    # enqueue and return immediately (adrs/python/celery-background-jobs.md)
    accepted = await client.post(f"/api/orders/{order_id}/receipt", headers=agent)
    assert accepted.status_code == 202
    task_id = accepted.json()["data"]["taskId"]
    assert task_id

    # status endpoint reads the result backend; with no worker consuming the
    # in-memory broker the task stays PENDING — the API contract is what this
    # test proves, the bridge is proven below
    status = await client.get(f"/api/orders/receipts/{task_id}", headers=agent)
    assert status.status_code == 200
    body = status.json()["data"]
    assert body["taskId"] == task_id
    assert body["state"] == "PENDING"
    assert body["receipt"] is None


async def test_receipt_request_enforces_ownership(client: AsyncClient, users: dict) -> None:
    admin = await _login(client, "admin@example.com", "Admin123!")
    agent = await _login(client, "agent@example.com", "Agent123!")
    other = await _login(client, "agent2@example.com", "Agent123!")
    product = await client.post(
        "/api/products", json={"sku": "SKU-RCPT-2", "name": "Widget"}, headers=admin
    )
    order = await client.post(
        "/api/orders",
        json={"productId": product.json()["data"]["id"], "quantity": 1},
        headers=agent,
    )
    order_id = order.json()["data"]["id"]

    response = await client.post(f"/api/orders/{order_id}/receipt", headers=other)
    assert response.status_code == 404


def test_receipt_task_bridges_into_the_async_service() -> None:
    """Executes the real task body — asyncio.run + shared session factory
    against the real database (adrs/python/celery-background-jobs.md). A sync
    test on purpose: the bridge owns its own event loop."""
    missing_order_id = str(uuid.uuid4())
    result = generate_order_receipt.apply(args=[missing_order_id])
    assert result.successful()
    assert result.get() == {"status": "not_found", "orderId": missing_order_id}
