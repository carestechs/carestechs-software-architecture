# Module-specific FastAPI dependencies live here as the module grows
# (adrs/python/modular-packages.md). Token VALIDATION dependencies are in
# app/core/auth.py so other modules can guard endpoints without importing
# this module.
