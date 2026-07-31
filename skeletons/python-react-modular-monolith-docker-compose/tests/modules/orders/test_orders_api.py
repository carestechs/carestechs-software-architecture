import uuid

from httpx import AsyncClient

PROBLEM = "application/problem+json"


async def _create_product(client: AsyncClient) -> str:
    response = await client.post("/api/products", json={"sku": "SKU-ORD", "name": "Widget"})
    assert response.status_code == 201
    return response.json()["data"]["id"]


async def test_create_order_resolves_product_through_the_contract(client: AsyncClient) -> None:
    product_id = await _create_product(client)

    created = await client.post("/api/orders", json={"productId": product_id, "quantity": 2})
    assert created.status_code == 201
    body = created.json()["data"]
    assert body["productId"] == product_id
    assert body["productName"] == "Widget"  # resolved via app.contracts.catalog, not a join
    assert body["quantity"] == 2

    fetched = await client.get(f"/api/orders/{body['id']}")
    assert fetched.status_code == 200
    assert fetched.json()["data"]["productName"] == "Widget"


async def test_order_for_unknown_product_is_a_not_found_problem(client: AsyncClient) -> None:
    response = await client.post(
        "/api/orders", json={"productId": str(uuid.uuid4()), "quantity": 1}
    )
    assert response.status_code == 404
    assert response.headers["content-type"].startswith(PROBLEM)
    assert response.json()["title"] == "Not Found"


async def test_missing_order_is_a_not_found_problem(client: AsyncClient) -> None:
    response = await client.get(f"/api/orders/{uuid.uuid4()}")
    assert response.status_code == 404
    assert response.headers["content-type"].startswith(PROBLEM)


async def test_invalid_quantity_is_a_validation_problem(client: AsyncClient) -> None:
    product_id = await _create_product(client)
    response = await client.post("/api/orders", json={"productId": product_id, "quantity": 0})
    assert response.status_code == 422
    assert response.headers["content-type"].startswith(PROBLEM)
    assert "quantity" in {error["field"] for error in response.json()["errors"]}
