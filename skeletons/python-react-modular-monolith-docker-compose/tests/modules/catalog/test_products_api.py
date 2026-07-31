import uuid

from httpx import AsyncClient

PROBLEM = "application/problem+json"


async def test_create_and_list_products(client: AsyncClient) -> None:
    created = await client.post("/api/products", json={"sku": "SKU-1", "name": "Widget"})
    assert created.status_code == 201
    body = created.json()
    assert body["data"]["sku"] == "SKU-1"
    assert "createdAt" in body["data"]  # camelCase JSON per the profile conventions

    listed = await client.get("/api/products")
    assert listed.status_code == 200
    body = listed.json()
    assert [p["name"] for p in body["data"]] == ["Widget"]
    assert body["meta"]["totalCount"] == 1  # adrs/api/rest-envelope.md


async def test_duplicate_sku_is_a_conflict_problem(client: AsyncClient) -> None:
    payload = {"sku": "SKU-DUP", "name": "First"}
    assert (await client.post("/api/products", json=payload)).status_code == 201

    duplicate = await client.post("/api/products", json={"sku": "SKU-DUP", "name": "Second"})
    assert duplicate.status_code == 409
    assert duplicate.headers["content-type"].startswith(PROBLEM)
    assert duplicate.json()["title"] == "Conflict"  # adrs/python/rfc7807-errors.md


async def test_missing_product_is_a_not_found_problem(client: AsyncClient) -> None:
    response = await client.get(f"/api/products/{uuid.uuid4()}")
    assert response.status_code == 404
    assert response.headers["content-type"].startswith(PROBLEM)
    assert response.json()["status"] == 404


async def test_validation_error_is_a_problem_with_field_details(client: AsyncClient) -> None:
    response = await client.post("/api/products", json={"sku": "", "name": ""})
    assert response.status_code == 422
    assert response.headers["content-type"].startswith(PROBLEM)
    fields = {error["field"] for error in response.json()["errors"]}
    assert fields == {"sku", "name"}
