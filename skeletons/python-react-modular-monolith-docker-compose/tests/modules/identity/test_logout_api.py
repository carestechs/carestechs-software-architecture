from httpx import AsyncClient

CSRF_HEADER = {"X-Requested-With": "XMLHttpRequest"}


async def test_logout_revokes_the_refresh_family(client: AsyncClient, users: dict) -> None:
    login = await client.post(
        "/api/auth/login", json={"email": "agent@example.com", "password": "Agent123!"}
    )
    assert login.status_code == 200
    refresh_cookie = login.cookies["refresh_token"]

    # CSRF guard applies to logout exactly as to refresh
    assert (await client.post("/api/auth/logout")).status_code == 403

    logged_out = await client.post("/api/auth/logout", headers=CSRF_HEADER)
    assert logged_out.status_code == 204
    # the cookie is cleared on the way out
    cleared = logged_out.headers["set-cookie"]
    assert "refresh_token=" in cleared and "Max-Age=0" in cleared

    # the revoked family can no longer refresh
    client.cookies.set("refresh_token", refresh_cookie, path="/api/auth")
    refreshed = await client.post("/api/auth/refresh", headers=CSRF_HEADER)
    assert refreshed.status_code == 401


async def test_logout_without_a_cookie_is_a_no_op(client: AsyncClient, users: dict) -> None:
    response = await client.post("/api/auth/logout", headers=CSRF_HEADER)
    assert response.status_code == 204
