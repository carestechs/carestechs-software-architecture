from httpx import AsyncClient

PROBLEM = "application/problem+json"


async def _admin(client: AsyncClient) -> dict[str, str]:
    response = await client.post(
        "/api/auth/login", json={"email": "admin@example.com", "password": "Admin123!"}
    )
    assert response.status_code == 200
    return {"Authorization": f"Bearer {response.json()['data']['accessToken']}"}


async def _seed_products(client: AsyncClient, headers: dict[str, str]) -> None:
    for sku, name in [("SKU-A", "Alpha"), ("SKU-B", "Beta"), ("SKU-C", "Gamma")]:
        created = await client.post(
            "/api/products", json={"sku": sku, "name": name}, headers=headers
        )
        assert created.status_code == 201


async def test_pagination_slices_and_reports_meta(client: AsyncClient, users: dict) -> None:
    await _seed_products(client, await _admin(client))

    first = await client.get("/api/products?page=1&pageSize=2")
    assert first.status_code == 200
    body = first.json()
    assert len(body["data"]) == 2
    assert body["meta"] == {"totalCount": 3, "page": 1, "pageSize": 2}

    second = await client.get("/api/products?page=2&pageSize=2")
    assert len(second.json()["data"]) == 1
    assert second.json()["meta"]["page"] == 2


async def test_sorting_is_allowlisted(client: AsyncClient, users: dict) -> None:
    await _seed_products(client, await _admin(client))

    descending = await client.get("/api/products?sortBy=name&sortDir=desc")
    names = [p["name"] for p in descending.json()["data"]]
    assert names == ["Gamma", "Beta", "Alpha"]

    # raw client input never reaches ORDER BY (adrs/api/offset-pagination.md)
    unknown = await client.get("/api/products?sortBy=password_hash")
    assert unknown.status_code == 400
    assert unknown.headers["content-type"].startswith(PROBLEM)


async def test_page_size_is_capped_at_100(client: AsyncClient, users: dict) -> None:
    response = await client.get("/api/products?pageSize=101")
    assert response.status_code == 422
    assert response.headers["content-type"].startswith(PROBLEM)
