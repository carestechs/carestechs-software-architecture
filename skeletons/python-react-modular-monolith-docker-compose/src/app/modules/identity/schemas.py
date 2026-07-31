from pydantic import BaseModel, ConfigDict, Field
from pydantic.alias_generators import to_camel


class LoginRequest(BaseModel):
    email: str = Field(min_length=3, max_length=200)
    password: str = Field(min_length=1, max_length=72)  # bcrypt input bound


class TokenRead(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True)

    access_token: str
    token_type: str = "Bearer"
    expires_in: int
