# Lab 2: FinanceApi Global Error Handling

```bash
dotnet run
```

Swagger UI opens at `/swagger` (see `launchUrl` in `Properties/launchSettings.json`).

## Endpoints

All in `Controllers/AccountsController.cs`.

* `GET /api/accounts`: 200
* `GET /api/accounts/error`: throws `NotImplementedException`, 500
* `GET /api/accounts/notfound`: throws `KeyNotFoundException`, 500
* `GET /api/accounts/invalid`: throws `ArgumentException`, 500
* `GET /api/accounts/{id:int}`: throws `NotFoundException`, 404
* `GET /api/accounts/validate`: throws `ValidationException` with a field error, 400

## Error Handling

* Middleware: `Middleware/ErrorHandlingMiddleware.cs`, `InvokeAsync` wraps `_next(context)` in try/catch, delegates to `HandleExceptionAsync`
* Registered first in the pipeline: `Program.cs`, `app.UseMiddleware<ErrorHandlingMiddleware>();`
* Response format: `HandleExceptionAsync` builds a `ProblemDetails` with `Status`, `Title`, `Detail`, `Instance` (request path); `traceId` goes in `Extensions` since it isn't a standard RFC 7807 field; content type `application/problem+json`
* Custom exception types: `Exceptions/NotFoundException.cs`, `Exceptions/ValidationException.cs`
* Mapping, in `HandleExceptionAsync`:

| Exception | Status |
|---|---|
| `NotFoundException` | 404 |
| `ValidationException` | 400 |
| anything else | 500 |

* 500 responses use a fixed generic `Detail` string, not `exception.Message` — avoids leaking internal exception info to clients

## Stretch: Structured Logging

* `Serilog.AspNetCore` 10.0.0, console sink, wired in `Program.cs` via `builder.Host.UseSerilog(...)`
* `ErrorHandlingMiddleware.InvokeAsync` logs every caught exception with `_logger.LogError`, `TraceId` as a structured field — correlates a log line to the `traceId` in the response body

## Stretch: ValidationException Field Errors

* `Exceptions/ValidationException.cs`: constructor takes `IDictionary<string, string[]>`, exposed as `Errors`
* `HandleExceptionAsync` adds them to `problemDetails.Extensions["errors"]` when present
* Demo: `GET /api/accounts/validate`, sample `amount` field error

## Stretch: Swagger Example Responses

* `Swagger/ErrorResponseExamplesFilter.cs`: `IOperationFilter`, attaches a sample `ProblemDetails` body to any 400/404/500 response already declared via `[ProducesResponseType]`
* Registered in `Program.cs`: `options.OperationFilter<ErrorResponseExamplesFilter>();`
* `[ProducesResponseType]` attributes: `Controllers/AccountsController.cs`, class-level 500, per-action 404/400/200

## Curl requests for Manual Tests

```bash
curl http://localhost:5001/api/accounts

curl -i http://localhost:5001/api/accounts/error

curl -i http://localhost:5001/api/accounts/notfound

curl -i http://localhost:5001/api/accounts/invalid

curl -i http://localhost:5001/api/accounts/999

curl -i http://localhost:5001/api/accounts/validate
```

| Case | Result |
|---|---|
| GET all | 200 |
| GET `error` | 500, generic `Detail`, `NotImplementedException` logged with `traceId` |
| GET `notfound` | 500, `KeyNotFoundException` isn't mapped to a custom type, falls to the 500 branch |
| GET `invalid` | 500, `ArgumentException` isn't mapped to a custom type, falls to the 500 branch |
| GET `999` | 404, `NotFoundException`, `Detail` includes the id |
| GET `validate` | 400, `ValidationException`, `errors.amount` in the body |

## Quick Reflection

**Why standardize error responses:** Clients parse one shape instead of branching on framework-specific stack-trace HTML, plain text, or ad-hoc JSON per endpoint. For a financial services API this also closes a security gap — before the middleware, unhandled exceptions returned raw `.NET` stack traces to the caller.

**Custom exception vs. generic `Exception`:** A generic `Exception` carries no intent — the middleware can't tell a bug from an expected condition, so everything maps to 500. `NotFoundException` and `ValidationException` are thrown deliberately for conditions the API anticipates, so `HandleExceptionAsync` can map them to the right status code (404, 400) instead of defaulting to 500 for everything.

**How Problem Details helps consumers:** Fixed fields (`status`, `title`, `detail`, `instance`) mean a client writes one parser for every error the API returns, instead of one per endpoint. `application/problem+json` also lets HTTP-aware tooling (browsers, API clients) recognize it's an error body without parsing the payload first.