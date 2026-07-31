from httpx import AsyncClient

PROBLEM = "application/problem+json"
CSRF_HEADER = {"X-Requested-With": "XMLHttpRequest"}


async def test_login_returns_token_and_refresh_cookie(client: AsyncClient, users: dict) -> None:
    response = await client.post(
        "/api/auth/login", json={"email": "agent@example.com", "password": "Agent123!"}
    )
    assert response.status_code == 200
    data = response.json()["data"]
    assert data["tokenType"] == "Bearer"
    assert data["expiresIn"] == 900  # 15 minutes (adrs/api/jwt-bearer-auth.md)
    assert data["accessToken"].count(".") == 2

    cookie = response.headers["set-cookie"]
    assert "refresh_token=" in cookie
    assert "HttpOnly" in cookie
    assert "samesite=strict" in cookie.lower()
    assert "Path=/api/auth" in cookie


async def test_wrong_password_is_a_401_problem(client: AsyncClient, users: dict) -> None:
    response = await client.post(
        "/api/auth/login", json={"email": "agent@example.com", "password": "nope"}
    )
    assert response.status_code == 401
    assert response.headers["content-type"].startswith(PROBLEM)
    assert response.headers["www-authenticate"] == "Bearer"


async def test_refresh_rotates_and_reuse_revokes_the_family(
    client: AsyncClient, users: dict
) -> None:
    login = await client.post(
        "/api/auth/login", json={"email": "agent@example.com", "password": "Agent123!"}
    )
    first_refresh = login.cookies["refresh_token"]

    # CSRF guard: cookie alone is not enough
    no_header = await client.post("/api/auth/refresh")
    assert no_header.status_code == 403

    rotated = await client.post("/api/auth/refresh", headers=CSRF_HEADER)
    assert rotated.status_code == 200
    second_refresh = rotated.cookies["refresh_token"]
    assert second_refresh != first_refresh

    # Reusing the ALREADY-ROTATED first token must revoke the whole family
    client.cookies.set("refresh_token", first_refresh, path="/api/auth")
    reuse = await client.post("/api/auth/refresh", headers=CSRF_HEADER)
    assert reuse.status_code == 401

    # ... including the otherwise-valid second token
    client.cookies.set("refresh_token", second_refresh, path="/api/auth")
    after_revoke = await client.post("/api/auth/refresh", headers=CSRF_HEADER)
    assert after_revoke.status_code == 401


async def test_product_write_requires_the_admin_role(client: AsyncClient, users: dict) -> None:
    payload = {"sku": "SKU-AUTH", "name": "Widget"}

    anonymous = await client.post("/api/products", json=payload)
    assert anonymous.status_code == 401
    assert anonymous.headers["content-type"].startswith(PROBLEM)

    agent = await _login(client, "agent@example.com", "Agent123!")
    forbidden = await client.post("/api/products", json=payload, headers=agent)
    assert forbidden.status_code == 403
    assert forbidden.headers["content-type"].startswith(PROBLEM)

    admin = await _login(client, "admin@example.com", "Admin123!")
    created = await client.post("/api/products", json=payload, headers=admin)
    assert created.status_code == 201


async def _login(client: AsyncClient, email: str, password: str) -> dict[str, str]:
    response = await client.post("/api/auth/login", json={"email": email, "password": password})
    assert response.status_code == 200
    return {"Authorization": f"Bearer {response.json()['data']['accessToken']}"}

