import uuid

from httpx import AsyncClient

PROBLEM = "application/problem+json"


async def _login(client: AsyncClient, email: str, password: str) -> dict[str, str]:
    response = await client.post("/api/auth/login", json={"email": email, "password": password})
    assert response.status_code == 200
    return {"Authorization": f"Bearer {response.json()['data']['accessToken']}"}


async def _create_product(client: AsyncClient, admin: dict[str, str]) -> str:
    response = await client.post(
        "/api/products", json={"sku": "SKU-ORD", "name": "Widget"}, headers=admin
    )
    assert response.status_code == 201
    return response.json()["data"]["id"]


async def test_create_order_resolves_product_through_the_contract(
    client: AsyncClient, users: dict
) -> None:
    admin = await _login(client, "admin@example.com", "Admin123!")
    agent = await _login(client, "agent@example.com", "Agent123!")
    product_id = await _create_product(client, admin)

    created = await client.post(
        "/api/orders", json={"productId": product_id, "quantity": 2}, headers=agent
    )
    assert created.status_code == 201
    body = created.json()["data"]
    assert body["productId"] == product_id
    assert body["productName"] == "Widget"  # resolved via app.contracts.catalog, not a join
    assert body["createdBy"] == str(users["agent"].id)  # stamped from claims, not the body

    fetched = await client.get(f"/api/orders/{body['id']}", headers=agent)
    assert fetched.status_code == 200
    assert fetched.json()["data"]["productName"] == "Widget"


async def test_orders_require_authentication(client: AsyncClient, users: dict) -> None:
    response = await client.post(
        "/api/orders", json={"productId": str(uuid.uuid4()), "quantity": 1}
    )
    assert response.status_code == 401
    assert response.headers["content-type"].startswith(PROBLEM)


async def test_order_ownership_is_enforced_in_the_service(
    client: AsyncClient, users: dict
) -> None:
    admin = await _login(client, "admin@example.com", "Admin123!")
    agent = await _login(client, "agent@example.com", "Agent123!")
    other = await _login(client, "agent2@example.com", "Agent123!")
    product_id = await _create_product(client, admin)

    created = await client.post(
        "/api/orders", json={"productId": product_id, "quantity": 1}, headers=agent
    )
    order_id = created.json()["data"]["id"]

    # another agent gets a 404 — same response as "does not exist", so IDs leak nothing
    assert (await client.get(f"/api/orders/{order_id}", headers=other)).status_code == 404
    # the owner and an admin both succeed
    assert (await client.get(f"/api/orders/{order_id}", headers=agent)).status_code == 200
    assert (await client.get(f"/api/orders/{order_id}", headers=admin)).status_code == 200


async def test_order_for_unknown_product_is_a_not_found_problem(
    client: AsyncClient, users: dict
) -> None:
    agent = await _login(client, "agent@example.com", "Agent123!")
    response = await client.post(
        "/api/orders", json={"productId": str(uuid.uuid4()), "quantity": 1}, headers=agent
    )
    assert response.status_code == 404
    assert response.headers["content-type"].startswith(PROBLEM)
    assert response.json()["title"] == "Not Found"


async def test_invalid_quantity_is_a_validation_problem(
    client: AsyncClient, users: dict
) -> None:
    admin = await _login(client, "admin@example.com", "Admin123!")
    agent = await _login(client, "agent@example.com", "Agent123!")
    product_id = await _create_product(client, admin)
    response = await client.post(
        "/api/orders", json={"productId": product_id, "quantity": 0}, headers=agent
    )
    assert response.status_code == 422
    assert response.headers["content-type"].startswith(PROBLEM)
    assert "quantity" in {error["field"] for error in response.json()["errors"]}
