# Multi-stage backend build (adrs/deployment/docker-multi-stage-builds.md)
FROM python:3.12 AS build
WORKDIR /build
COPY pyproject.toml ./
COPY src ./src
RUN pip wheel --wheel-dir /wheels .

FROM python:3.12-slim
WORKDIR /srv
COPY --from=build /wheels /wheels
RUN pip install --no-cache-dir --no-index --find-links=/wheels app && rm -rf /wheels
EXPOSE 8000
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
