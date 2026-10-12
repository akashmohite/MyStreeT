# Test Report

**Date:** 2026-10-07 · **Environment:** Linux sandbox, Node v22.22.2, .NET SDK 8.0.131 (Ubuntu package), headless Chrome 131.

> **Honest summary:** Frontend: built, unit-tested, audited and driven end-to-end in a real browser — but against a **mock API**, not the .NET backend. Backend: **NOT built, NOT tested, NOT run** because `dotnet restore` cannot reach NuGet in this sandbox. The backend verdict is therefore *unverified*, not *passed*.

## 1. Backend build — **NOT EXECUTED (blocked)**
```
$ dotnet restore  →  error : Unable to load the service index for source https://api.nuget.org/v3/index.json
                     Response status code does not indicate success: 403 (Forbidden)   [x-deny-reason: host_not_allowed]
```
`dotnet build`, `dotnet test`, EF migrations, SQL Server startup and Swagger were **not run**.

### Partial backend checks that *were* run
| Check | Method | Result |
|---|---|---|
| Type-check of Entities, DTOs, Services, Controllers, Common, Auth (except JWT handler) | Compiled with the .NET 8 compiler against hand-written **stubs** of EF Core/BCrypt (library build) | **0 errors, 0 warnings.** Not covered: `Program.cs`, `JwtTokenService.cs`, `AppDbContext` fluent config (stubbed), the test project. |
| `Validators` logic (email, password, sizes, image URL, phone, postal) | Compiled & **executed** in a console harness | **25/25 checks passed** (mirrors the `ValidatorsTests` cases; this is *not* an xUnit run). |
| EF query translation (brand/size filters, `Include`), BCrypt, JWT creation, DI wiring, middleware pipeline, Swagger | – | **Not verified.** |

## 2. Backend unit tests — **NOT EXECUTED**
51 test methods written (40 `[Fact]` + 11 `[Theory]` with 42 data rows): Auth (8), JWT (3), Products (14), Orders (16), Validators (7), Error middleware (3). Coverage **not measured**; the ≥60% target is unconfirmed. Command: `dotnet test --collect:"XPlat Code Coverage"`.
Risk: these tests were never compiled against the real packages; expect the possibility of small compile/assertion fixes on first run.

## 3. Frontend — executed
| Command | Result |
|---|---|
| `npm install` | OK |
| `npm audit` | **0 vulnerabilities** (initially 8 incl. react-router open-redirect advisory and vitest/vite dev-tool advisories → fixed by upgrading to react-router-dom 7.18, Vite 8.3, Vitest 5.0, plugin-react 6.1) |
| `npm run build` (`tsc -b && vite build`) | **Succeeded**, no TS errors (JS 270 kB / 88 kB gzip) |
| `npm test` (Vitest) | **16 passed / 16** (cart reducer, `loadCart`, form validation) |

## 4. UI end-to-end scenarios — executed against a MOCK API
Setup: production build served by `vite preview`; `tests/ui-e2e/mock-api.mjs` re-implements the API contract (routes, validation, 401/403/404/409 behaviour, ownership, structured errors). Driven by Puppeteer + Chrome 131. **Result: 31/31 passed.**

| Scenario | Checked | Result |
|---|---|---|
| 1 Guest browsing | ≥8 products; brand filter (Nike → 3); size filter (11) incl. URL persistence after refresh; empty-state path; detail page; no broken images | PASS |
| 2 Registration | Register → signed in → refresh keeps session | PASS |
| 3 Cart | Missing-size blocked; add two products; qty change → total 379.97; refresh keeps cart & total; empty state on removing all | PASS |
| 4 Checkout | Empty form errors; invalid phone; Mock UPI order; failed order keeps the cart | PASS |
| 5 Confirmation | "Order successfully placed", Order ID, PLACED, items, total, shipping, payment | PASS |
| 6 Order history | List + detail match Order ID; refresh stable | PASS |
| 7 Admin | Login; validation errors; create → in catalog; edit → catalog shows change; delete → gone | PASS |
| 8 Security | User → "Access denied" page; direct API calls: user POST/PUT/DELETE → 403, anonymous → 401; other user's order → 404; guest redirected from /orders, /checkout | PASS |
| 9 Invalid input | Login, registration (weak/mismatch/duplicate), product (400 structured), order (400), 404; client prices ignored | PASS |
| 10 Refresh / direct navigation | Product details, cart, orders, admin, unknown product/route; invalid token → signed out; mobile menu, no horizontal overflow; **no console/page errors during the run** | PASS |

Caveat: these prove the **UI and its use of the API contract**. The mock was written by the same author as the backend, so it can encode the same misunderstandings; it is not proof the .NET API behaves identically. One test ("no-match filter") asserts loosely and would pass either way.

## 5. Postman collection — executed against the MOCK API
`newman run postman/MyStreeT.postman_collection.json` → **37 requests, 37 test-scripts, 72 assertions, 0 failed.** Not run against the real API.

## 6. Failures discovered and fixes made during this session
| Finding | Fix |
|---|---|
| Vitest/Vite `test` config type error after upgrade | Import `defineConfig` from `vitest/config` |
| 8 npm advisories | Upgraded react-router-dom, Vite, Vitest, plugin-react (0 remaining) |
| Test data bug found on review: `ContainSingle` for size 11 would match 2 products | Corrected the assertion in `ProductServiceTests` |
| `sh` brace expansion created wrong folders | Recreated the directory structure |

## 7. Remaining limitations
1. **Backend never built or run** (NuGet blocked). First action on a normal machine: `dotnet restore && dotnet build && dotnet test`.
2. EF migration files not generated (`dotnet ef migrations add InitialCreate`); app uses `EnsureCreated` until then.
3. Coverage not measured; `dotnet list package --vulnerable` not run.
4. Visual design not reviewed via screenshots.
5. No real concurrency control for stock; no rate limiting; JWT in localStorage.
6. Real-DB behaviours (unique email index, FK restrict on product delete, decimal precision) untested.
