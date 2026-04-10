# CLAUDE.md — Portal eUprava (Pilot JLS)

> **Verzija:** 1.3
> **Autor:** Agent #2 (Arhitekt)
> **Za:** Agent #1 (Implementer) i sve buduće Claude instance koje rade na repu
> **Kako čitati:** Ovo je priručnik, ne roman. Svaka sekcija je samostalna. Sadržaj na vrhu, ⚠️ TODO sekcija na dnu.

---

## Sadržaj

1. Što je ovo
2. Fiksne tehničke odluke
3. Pojmovnik (terminološka pravila — obvezna)
4. Poslovni tok
5. Struktura repozitorija
6. Portal baza (PostgreSQL)
7. State machine zahtjeva
8. Integracijski sloj (SEUP)
9. Outbox pattern i idempotencija
10. API endpointi
11. Admin modul (vrste zahtjeva)
12. Građanski flow
13. Back-office (službenica)
14. Notifikacije
15. Background workeri
16. Frontend napomene
17. Sigurnost
18. Testiranje
19. Deployment
20. Pravila rada u repu
21. ⚠️ TODO — čeka input
22. Faza 2 backlog

---

## 1. Što je ovo

Portal za komunikaciju građana s jedinicom lokalne samouprave (JLS). Tanki sloj iznad postojećeg SEUP-a (Sustav uredskog poslovanja) koji JLS već koristi.

**Portal NE radi uredsko poslovanje.** Portal je:
- ulazna točka za građane (podnošenje zahtjeva, pregled financija)
- pregledni alat za službenike (inbox, zaprimanje u pisarnicu)
- konfiguracijski alat za admina (vrste zahtjeva, polja, privitci)

SEUP i modul financija JLS-a žive u **jednoj SQL Server bazi** na lokalnom serveru JLS-a. Portal ima vanjski pristup (VPN/fiksni IP + firewall).

**Važno:** SEUP je naš vlastiti proizvod — mi ga razvijamo. To znači da imamo puni pristup SEUP bazi, možemo dodavati stupce i pisati stored procedure. Nema pregovaranja s trećom stranom za shema promjene.

---

## 2. Fiksne tehničke odluke

| Područje | Odluka |
|---|---|
| Backend | .NET 9, C# 13 |
| Arhitektura | Clean Architecture (Domain / Application / Infrastructure / Api) |
| Aplikacijski uzorak | CQRS + MediatR |
| Validacija | FluentValidation u Application sloju |
| Portal DB | PostgreSQL 16+, Npgsql + EF Core 9 |
| SEUP DB | SQL Server (tuđa, read + targeted write), Dapper u dedikiranom adapteru |
| Frontend | React 18 + TypeScript + Vite, Tailwind, react-i18next, TanStack Query, react-hook-form + zod |
| Auth | JWT access + refresh token, BCrypt (work factor 12) |
| Container | Docker + docker-compose |
| CI/CD | GitHub Actions |
| Multi-tenant | Od dana 1 (svi redovi `tenant_id`, RLS) |
| i18n | Od dana 1, primarni `hr`, struktura spremna za `en` |
| Mobile | Mobile-first (građanski), responsive (back-office) |
| Errors | RFC 7807 ProblemDetails |
| Logging | Serilog |
| Testing | xUnit + FluentAssertions + Testcontainers |
| PDF preview | PDF.js (inline u back-officeu) |

---

## 3. Pojmovnik (terminološka pravila — obvezna)

Ovi pojmovi su pravno i operativno različiti. Miješanje stvara pravne i UX probleme. Pravila su obvezna u UI tekstovima, API DTO imenima i kodu.

| Pojam | Što je | Kad postoji | Tehnički |
|---|---|---|---|
| **Referentni broj zahtjeva** | Portalov interni identifikator zahtjeva | Od trenutka kreiranja drafta | `requests.reference_number`, format `ZHT-YYYY-NNNNNN`, DTO: `referenceNumber` |
| **Broj akta** | SEUP-ov identifikator zaprimljenog akta | Tek u stanju `received_in_registry` | `seup_akt_mappings.akt_id`, dolazi iz `tblAkti.AktID` (bigint), DTO: `aktId` |
| **Klasa** | SEUP-ov klasifikacijski oznaka predmeta | NE postoji u portalu uopće | Dodjeljuje se kasnije u SEUP-u, izvan našeg scope-a |

**Zabranjeno:**
- Zvati referentni broj "klasom" ili "brojem akta"
- Prikazivati `aktId` građaninu prije nego je status `received_in_registry`
- Imati polje `klasa_id` ili `caseClass` bilo gdje u portal kodu
- Koristiti `caseNumber` u API DTO-ima — uvijek `referenceNumber` ili `aktId` ovisno o kontekstu

i18n review pri svakom PR-u provjerava da hrvatski tekstovi poštuju pravila.

---

## 4. Poslovni tok

```
 ┌─────────────┐
 │  GRAĐANIN   │
 └──────┬──────┘
        │ popuni formu, uploada privitke, klikne "Pošalji"
        ▼
 ┌─────────────────────────────────────┐
 │ PORTAL DB (PostgreSQL)              │
 │ requests.status = 'submitted'       │
 │ + email "Zahtjev podnesen"          │
 └──────┬──────────────────────────────┘
        │
        │ službenica otvori back-office inbox
        ▼
 ┌─────────────────────────────────────┐
 │  SLUŽBENICA pregleda zahtjev        │
 └──────┬──────────────────────────────┘
        │
        ├─── "Zaprimi u pisarnicu" ───┐
        │                              │
        │                              ▼
        │                  ┌───────────────────────────────┐
        │                  │ OUTBOX → SEUP SQL Server      │
        │                  │ INSERT tblAkti (vraća AktID)  │
        │                  │ INSERT tblPRBiljeska          │
        │                  │ INSERT tblDatoteke (n redova) │
        │                  │ + copy fajlova u intake folder│
        │                  └──────┬────────────────────────┘
        │                         │
        │                         ▼
        │                  ┌───────────────────────────┐
        │                  │ PORTAL DB:                │
        │                  │ seup_akt_mappings (1:1)   │
        │                  │ status='received_in_reg.' │
        │                  │ email građaninu + AktID   │
        │                  └───────────────────────────┘
        │
        └─── "Odbij" ─────────────────┐
                                       ▼
                        ┌───────────────────────────┐
                        │ PORTAL DB:                │
                        │ status='rejected_by_off.' │
                        │ razlog iz kataloga        │
                        │ email građaninu           │
                        └───────────────────────────┘
```

Nakon `received_in_registry` portal se povlači. Daljnja obrada (klasa, predmet, rješenje) odvija se u SEUP-u, izvan portala.

---

## 5. Struktura repozitorija

```
portal-euprava/
├── src/
│   ├── Portal.Domain/                    # entiteti, value objects, domain events
│   ├── Portal.Application/               # CQRS handleri, ports (interfaces), DTOs
│   ├── Portal.Infrastructure.Persistence/# EF Core DbContext, migracije
│   ├── Portal.Infrastructure.LocalDb/    # ⚠️ JEDINI projekt s SQL Server referencom
│   ├── Portal.Infrastructure.Storage/    # IAttachmentStorage implementacije
│   ├── Portal.Infrastructure.Email/      # IEmailSender implementacije
│   ├── Portal.Infrastructure.Identity/   # JWT, password hashing
│   └── Portal.Api/                       # ASP.NET Core minimal APIs, DI, middleware
├── tests/
│   ├── Portal.Domain.Tests/
│   ├── Portal.Application.Tests/
│   ├── Portal.Infrastructure.Tests/
│   ├── Portal.Api.IntegrationTests/      # Testcontainers
│   └── Portal.Architecture.Tests/        # NetArchTest
├── web/
│   ├── src/
│   │   ├── app/                          # router, providers
│   │   ├── features/
│   │   │   ├── auth/
│   │   │   ├── finance/
│   │   │   ├── requests/                 # građanin
│   │   │   ├── office/                   # službenica
│   │   │   └── admin/                    # admin
│   │   ├── shared/                       # UI kit, hooks, api client, dynamic form
│   │   └── i18n/
│   └── public/
├── docker/
│   ├── api.Dockerfile
│   ├── web.Dockerfile
│   └── docker-compose.yml                # api + web + postgres + mailhog
├── .github/workflows/
└── docs/
```

### Pravila ovisnosti (forsirano arhitekturnim testom)

```
Api              → Application, Infrastructure.*
Application      → Domain
Domain           → (ništa)
Infrastructure.* → Application, Domain
```

**`Microsoft.Data.SqlClient` smije postojati samo u `Portal.Infrastructure.LocalDb`.** Bilo gdje drugdje = build fail.

---

## 6. Portal baza (PostgreSQL)

Konvencije:
- `snake_case`
- PK = `id uuid` (default `gen_random_uuid()`)
- Svi poslovni redovi imaju `tenant_id uuid NOT NULL`
- `created_at timestamptz`, `updated_at timestamptz`
- Soft delete `deleted_at timestamptz NULL` gdje treba
- Row-Level Security uključen — middleware postavlja `SET app.tenant_id = '...'` po requestu

### 6.1 Tenancy i korisnici

```sql
tenants (
  id uuid PK,
  code varchar(32) UNIQUE,
  name varchar(200),
  oib varchar(11),
  settings jsonb,                   -- branding, default_processing_days, mail_from, ...
  is_active bool DEFAULT true,
  created_at, updated_at
)

users (
  id uuid PK,
  tenant_id uuid FK,
  email citext NOT NULL,
  password_hash varchar(255) NULL,  -- NULL rezerv. za budući NIAS
  oib varchar(11) NULL,
  first_name varchar(100),
  last_name varchar(100),
  phone varchar(32) NULL,
  user_type varchar(20) NOT NULL
    CHECK (user_type IN ('citizen','jls_officer','jls_admin')),
  seup_subject_id bigint NULL,              -- SubjektID iz tblDatSubjekti, NULL dok nije odobrena e-komunikacija
  email_verified_at timestamptz NULL,
  last_login_at timestamptz NULL,
  is_active bool DEFAULT true,
  preferred_language varchar(5) DEFAULT 'hr',
  created_at, updated_at, deleted_at,
  UNIQUE (tenant_id, email)
)

roles (id, tenant_id, code, name)
user_roles (user_id, role_id)

refresh_tokens (id, user_id, token_hash, expires_at, revoked_at, user_agent, ip)
email_verification_tokens (id, user_id, token_hash, expires_at, used_at)
password_reset_tokens (id, user_id, token_hash, expires_at, used_at)
```

### 6.2 Vrste zahtjeva (admin konfiguracija)

```sql
request_types (
  id uuid PK,
  tenant_id uuid FK,
  code varchar(64),
  name_i18n jsonb,                          -- {"hr":"...","en":"..."}
  description_i18n jsonb,
  is_active bool DEFAULT true,
  is_archived bool DEFAULT false,
  sort_order int,
  version int DEFAULT 1,                    -- bumpa se na strukturnu promjenu
  estimated_processing_days int NULL,       -- NULL → fallback na tenants.settings.default_processing_days → 5
  is_ecommunication_request bool DEFAULT false,  -- true = posebna vrsta "Zahtjev za e-komunikaciju", accept flow ažurira users.seup_subject_id umjesto upisa u tblAkti
  created_at, updated_at, deleted_at,
  UNIQUE (tenant_id, code)
)

request_type_fields (
  id uuid PK,
  request_type_id uuid FK,
  field_key varchar(64),
  label_i18n jsonb,
  help_text_i18n jsonb NULL,
  field_type varchar(20)
    CHECK (field_type IN ('text','textarea','number','date',
                          'select','checkbox','oib','iban','email','phone')),
  is_required bool,
  validation_rules jsonb NULL,              -- {"min":0,"max":100,"regex":"..."}
  options jsonb NULL,                       -- za select: [{value, label_i18n}]
  sort_order int,
  UNIQUE (request_type_id, field_key)
)

request_type_attachments (
  id uuid PK,
  request_type_id uuid FK,
  attachment_key varchar(64),
  label_i18n jsonb,
  description_i18n jsonb NULL,
  is_required bool,
  max_size_bytes bigint,
  allowed_mime_types text[],
  sort_order int
)
```

**Bez `klasa_id`.** Vidi sekciju 3 (Pojmovnik).

### 6.3 Podneseni zahtjevi

```sql
requests (
  id uuid PK,
  tenant_id uuid FK,
  citizen_id uuid FK,
  request_type_id uuid FK,
  request_type_version int,                 -- snapshot verzije sheme
  reference_number varchar(32) UNIQUE,      -- 'ZHT-2026-000123'
  status varchar(32) NOT NULL,
  form_data jsonb NOT NULL,
  form_schema_snapshot jsonb NOT NULL,      -- snapshot fields+attachments definicija
  submitted_at timestamptz NULL,
  reviewed_by_user_id uuid FK NULL,
  reviewed_at timestamptz NULL,
  rejection_reason_code varchar(50) NULL,
  rejection_internal_note text NULL,
  viewed_first_at timestamptz NULL,         -- prvi put kad je officer otvorio
  viewed_first_by_user_id uuid FK NULL,
  expires_at timestamptz NULL,              -- za draftove (90d sliding ili 30d locked)
  is_locked_to_old_version bool DEFAULT false,  -- true ako je shema bumped a draft ostao na staroj
  etag varchar(64),                          -- za optimistic concurrency na PATCH
  created_at, updated_at
)

request_attachments (
  id uuid PK,
  request_id uuid FK,
  attachment_key varchar(64),
  original_filename varchar(255),
  mime_type varchar(100),
  size_bytes bigint,
  storage_key varchar(500),
  checksum_sha256 varchar(64),
  uploaded_at timestamptz,
  uploaded_by_user_id uuid FK
)

request_status_history (
  id uuid PK,
  request_id uuid FK,
  from_status varchar(32) NULL,
  to_status varchar(32) NOT NULL,
  changed_by_user_id uuid FK NULL,
  changed_by_source varchar(20)
    CHECK (changed_by_source IN ('citizen','officer','system')),
  comment text NULL,
  changed_at timestamptz
)
```

### 6.4 Mapping portal ↔ SEUP

```sql
seup_akt_mappings (
  id uuid PK,
  tenant_id uuid FK,
  request_id uuid FK UNIQUE,                -- 1:1, JEDAN red po zahtjevu
  akt_id bigint NOT NULL,                   -- AktID iz tblAkti.AktID
  received_at timestamptz,
  received_by_user_id uuid FK,
  created_at,
  UNIQUE (tenant_id, request_id)            -- belt + braces, vidi sekciju 9
)
```

**`UNIQUE (request_id)` je ključni dio idempotencije.** Vidi sekciju 9.

### 6.5 Outbox

```sql
integration_outbox (
  id uuid PK,
  tenant_id uuid FK,
  aggregate_type varchar(50),               -- 'request'
  aggregate_id uuid,                        -- request.id
  operation varchar(50),                    -- 'write_akt_to_seup'
  idempotency_key varchar(100) UNIQUE,      -- = request.id (string), garantira 1 outbox po zahtjevu
  payload jsonb,
  status varchar(20)
    CHECK (status IN ('pending','processing','done','failed','dead_letter')),
  attempts int DEFAULT 0,
  last_error text NULL,
  next_attempt_at timestamptz,
  processed_at timestamptz NULL,
  created_at,
  INDEX (status, next_attempt_at)
)
```

### 6.6 Notifikacije

```sql
notifications (
  id uuid PK,
  tenant_id uuid FK,
  user_id uuid FK,
  type varchar(50),
  title_i18n jsonb,
  body_i18n jsonb,
  related_request_id uuid FK NULL,
  is_read bool DEFAULT false,
  read_at timestamptz NULL,
  created_at
)

notification_deliveries (
  id uuid PK,
  notification_id uuid FK,
  channel varchar(20),                      -- 'in_app','email'
  status varchar(20),                       -- 'pending','sent','failed','skipped'
  provider_message_id varchar(255) NULL,
  attempts int DEFAULT 0,
  last_error text NULL,
  sent_at timestamptz NULL
)
```

### 6.7 Financije (cache)

```sql
finance_snapshots (
  id uuid PK,
  tenant_id uuid FK,
  oib varchar(11),
  fetched_at timestamptz,
  expires_at timestamptz,                   -- +15 min default
  payload jsonb,
  INDEX (tenant_id, oib, expires_at)
)
```

### 6.8 Audit log

```sql
audit_log (
  id uuid PK,
  tenant_id uuid FK,
  user_id uuid NULL,
  action varchar(100),
  entity_type varchar(50),
  entity_id uuid NULL,
  before jsonb NULL,
  after jsonb NULL,
  ip varchar(45),
  user_agent text,
  created_at,
  INDEX (tenant_id, created_at DESC),
  INDEX (tenant_id, entity_type, entity_id)
)
```

---

## 7. State machine zahtjeva

```
         (građanin kreira)
              │
              ▼
          ┌────────┐  građanin uređuje (auto-save)
          │ draft  │◄────────────┐
          └───┬────┘             │
              │ submit           │
              ▼                  │
        ┌────────────┐           │
        │ submitted  │           │
        └──┬───────┬─┘           │
           │       │             │
  officer  │       │ officer     │
  "Zaprimi"│       │ "Odbij"     │
           ▼       ▼             │
  ┌──────────────┐ ┌──────────────────────┐
  │ processing_  │ │ rejected_by_officer  │
  │ registry     │ │                      │
  │ (interni)    │ │  (građanin može      │
  └──────┬───────┘ │   kreirati novi)     │
         │         └──────────────────────┘
  outbox │                  TERMINAL
  uspjeh │
         ▼
  ┌──────────────┐
  │ received_in_ │
  │ registry     │
  │ (+ akt_id)   │
  └──────────────┘
     TERMINAL
```

**Status vrijednosti:** `draft`, `submitted`, `processing_registry`, `received_in_registry`, `rejected_by_officer`.

> **VAŽNO za Implementer enum:** `processing_registry` je **interni intermediate status**. Postoji u bazi i u back-officeu (officer ga vidi kao "U procesiranju"), ali se **mapira u `submitted` u svim građanskim API odgovorima i UI prikazima**. Application sloj radi to mapiranje u DTO mapper-u (`CitizenRequestStatusMapper`). Bez ovog statusa idempotencija outboxa nema čisto stanje između "officer je kliknuo" i "akt je zapisan" — nemoj ga izostaviti iz enuma.

**Pravila prijelaza:**
- `draft → submitted`: samo vlasnik (citizen), samo ako su sva obvezna polja popunjena i obvezni privitci uploadani
- `submitted → received_in_registry`: samo officer, kroz outbox, atomski s upisom u `seup_akt_mappings`
- `submitted → rejected_by_officer`: samo officer, sinkrono, mora imati `rejection_reason_code`
- Iz terminalnih stanja nema prijelaza u MVP-u

**Draft expiration:**
- Pri kreiranju drafta: `expires_at = now + 90 dana`
- Svaki PATCH (auto-save) reset-a `expires_at = now + 90 dana` (sliding)
- Iznimka: ako je shema bump-ana i draft "zaključan na staru verziju", `is_locked_to_old_version = true` i `expires_at = now + 30 dana` (ne sliding)
- 7 dana prije isteka: in-app + email upozorenje
- Na dan isteka: hard delete (draft + privitci iz storage-a)

---

## 8. Integracijski sloj (SEUP)

### 8.1 Princip

Ports & Adapters. Core (Domain + Application) ne zna ništa o SQL Serveru. Komunikacija isključivo kroz interface-e definirane u `Portal.Application`.

### 8.2 Portovi

```csharp
public interface ILocalDbAktWriter
{
    Task<AktWriteResult> WriteAktAsync(
        WriteAktCommand cmd,
        CancellationToken ct);
}

public record WriteAktCommand(
    Guid TenantId,
    Guid RequestId,
    string IdempotencyKey,            // = RequestId.ToString()
    string CitizenOib,
    string CitizenFullName,
    string CitizenAddress,
    string CitizenEmail,
    long SeupSubjectId,               // iz users.seup_subject_id — obavezan, provjera prije poziva
    string Subject,
    string BodyText,
    DateTimeOffset ReceivedAt,
    IReadOnlyCollection<AktAttachmentInput> Attachments);

public record AktAttachmentInput(
    string OriginalFilename,
    string MimeType,
    long SizeBytes,
    string PortalStorageKey);

public record AktWriteResult(
    bool Success,
    long? AktId,
    string? ErrorCode,
    string? ErrorMessage,
    bool IsDuplicate);                // true ako je SEUP odbio zbog već postojećeg upisa

public interface IFinanceReader
{
    Task<FinanceSnapshot> GetByOibAsync(
        Guid tenantId, string oib, CancellationToken ct);
}

public interface IAttachmentStorage
{
    Task<string> SaveAsync(Stream content, string suggestedName, CancellationToken ct);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct);
    Task DeleteAsync(string storageKey, CancellationToken ct);
}

public interface IArchiveFileCopier
{
    Task<string> CopyToIntakeAsync(Stream source, string filename, CancellationToken ct);
    // vraća putanju koju upišemo u tblDatoteke
}

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct);
}
```

### 8.3 Adapter `LocalDbAktWriter`

Živi u `Portal.Infrastructure.LocalDb`. Koristi Dapper + `Microsoft.Data.SqlClient`.

**Tok unutar `WriteAktAsync`:**

1. Otvori SQL transakciju
2. Provjeri postoji li već akt za ovaj `PortalRequestID` (vidi 9.3 sloj 4):
   - Ako da → ROLLBACK, vrati `IsDuplicate=true` s postojećim `AktID`
   - Ako stupac `PortalRequestID` ne postoji na `tblAkti` → preskoči provjeru, log warning
3. Generiraj novi AktID: `SELECT ISNULL(MAX(AktID), 0) + 1 FROM tblAkti` (SEUP ne koristi IDENTITY — aplikacija dodjeljuje)
4. Za svaki privitak:
   - Otvori stream iz `IAttachmentStorage`
   - Pozovi `IArchiveFileCopier.CopyToIntakeAsync` → dobiješ putanju
5. `INSERT INTO tblAkti`:
   - `AktID` = generirani broj
   - `VrstaID` = 0 (ulazni dokument / podnesak)
   - `Opis` = naziv vrste zahtjeva + referentni broj (max 255 chars)
   - `DatumKreiranjaAkta` = danas
   - `DatumPrimitka` = danas
   - `PredmetID` = 0 (uvijek 0 kod primitka)
   - `StvarateljID` = 0 (uvijek 0 kod primitka)
   - `IDSesije` = NULL (za sad, riješit će se s SEUP launcher integracijom)
   - `PortalRequestID` = request.id (UUID) — ako stupac postoji
6. `INSERT INTO tblPRBiljeska`:
   - `BiljeskaID` = `SELECT ISNULL(MAX(BiljeskaID), 0) + 1 FROM tblPRBiljeska`
   - `Datum` = danas
   - `SubjektID` = iz `users.seup_subject_id` (građanin mora imati odobrenu e-komunikaciju)
   - `NacinBiljeske` = 3000 (fiksno za portal podnesak, šifarnik se proširuje kasnije)
   - `Opis` = opis zahtjeva (max 255 chars)
   - `AktID` = generirani AktID iz koraka 5
   - `IDSesije` = NULL (za sad)
7. Za svaki privitak: `INSERT INTO tblDatoteke`:
   - `DatotekaID` = `SELECT ISNULL(MAX(DatotekaID), 0) + 1 FROM tblDatoteke`
   - `Datoteka` = opis datoteke (attachment label, max 255)
   - `DatumDatoteke` = danas
   - `LokacijaDatoteke` = putanja iz `IArchiveFileCopier` (max 255)
   - `NazivDatoteke` = originalno ime fajla (max 255)
   - `AktID` = generirani AktID iz koraka 5
   - `RbrDatoteke` = redni broj privitka (1, 2, 3...)
   - `IDSesije` = NULL (za sad)
8. COMMIT
9. Vrati `AktWriteResult { Success=true, AktId=generirani AktID }`

**Na exception:**
- ROLLBACK SQL transakcije
- Best-effort cleanup kopiranih fajlova iz intake foldera (ako neki nisu još pokupljeni od arhive servisa). Ako i to padne — log kao warning, ručno čišćenje od strane admina.
- Vrati `AktWriteResult { Success=false, ErrorCode=..., ErrorMessage=... }`

⚠️ Točni stupci za INSERT-e — vidi gornji tok (v1.3, svi stupci su poznati).

**PortalRequestID startup check:**

Portal pri startupu provjerava postoji li stupac `PortalRequestID` na `tblAkti`:

```sql
SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'tblAkti' AND COLUMN_NAME = 'PortalRequestID'
```

- Ako postoji → idempotencija sloj 4 aktivan, log info
- Ako NE postoji → log WARNING: "PortalRequestID stupac nije pronađen na tblAkti — idempotencija sloj 4 nije aktivan. Dodajte stupac za punu zaštitu od duplikata."
- Portal i dalje radi, ali bez sloja 4 zaštite (prva tri sloja su i dalje aktivna)

### 8.4 Ostali adapteri

- `LocalDbFinanceReader` — SELECT po OIB-u, mapiranje u `FinanceSnapshot`
- `LocalFileSystemAttachmentStorage` — `/var/portal/attachments/{tenant}/{yyyy}/{mm}/{guid}` (default)
- `LocalArchiveFileCopier` — kopira u dogovoreni intake folder (⚠️ putanja čeka input)
- `SmtpEmailSender` — MailKit, konfigurabilan host/port/credentials (fallback)
- `ResendEmailSender` — Resend.net SDK, API ključ iz env var `RESEND_API_KEY` (produkcija)
- `NullEmailSender` — dev/test

---

## 9. Outbox pattern i idempotencija

> **Ova sekcija je kritična. Dvostruki upis istog zahtjeva u SEUP je katastrofa (dva akta za isti zahtjev = pravna i operativna noćna mora). Implementer mora razumjeti i poštovati svaki sloj zaštite.**

### 9.1 Zašto outbox

Officer klikne "Zaprimi u pisarnicu". Tri stvari mogu poći krivo:
- VPN prema lokalnom serveru pukne usred zapisa
- SQL Server restart u tom trenutku
- Officer slučajno klikne dvaput (network lag, anxious user)

Bez outboxa: ili gubimo akcije, ili stvaramo duplikate. Outbox pattern razdvaja korisničku akciju od fizičkog upisa i daje at-least-once semantiku, a idempotencija na drugoj strani pretvara to u efektivno exactly-once.

### 9.2 Tok

**Korak 1: Officer klikne "Zaprimi" → API endpoint**

```
POST /office/requests/{id}/accept
```

Handler u JEDNOJ PostgreSQL transakciji:

```sql
BEGIN;

-- 1. Provjeri da je status još uvijek 'submitted' (concurrency)
SELECT status FROM requests WHERE id = :id FOR UPDATE;
-- ako nije submitted → ROLLBACK + 409 Conflict

-- 2. Provjeri da već ne postoji outbox red za isti request
SELECT 1 FROM integration_outbox
  WHERE idempotency_key = :request_id::text
  AND status IN ('pending','processing','done');
-- ako postoji → ROLLBACK + 409 (već u procesu)

-- 3. Update statusa
UPDATE requests
  SET status = 'processing_registry',  -- interni intermediate, ne pokazuje se građaninu
      reviewed_by_user_id = :officer_id,
      reviewed_at = now()
  WHERE id = :id;

-- 4. Insert outbox
INSERT INTO integration_outbox (
  id, tenant_id, aggregate_type, aggregate_id,
  operation, idempotency_key, payload,
  status, attempts, next_attempt_at, created_at
) VALUES (
  gen_random_uuid(), :tenant_id, 'request', :id,
  'write_akt_to_seup', :id::text, :payload_json,
  'pending', 0, now(), now()
);

-- 5. Insert status history
INSERT INTO request_status_history (...) VALUES (...);

COMMIT;
```

**Napomena:** `processing_registry` se NE prikazuje građaninu. Građanin i dalje vidi `submitted` (mapping u Application sloju). Officer vidi "U procesiranju" indikator.

**Korak 2: Sync wait s timeout-om**

API handler nakon commita ne vraća odmah. Pokušava sinkroni dispatch s timeout-om:

```csharp
var dispatchTask = _outboxDispatcher.TryDispatchNowAsync(outboxId, ct);
var timeoutTask = Task.Delay(TimeSpan.FromSeconds(12), ct);
// 12s prvi tjedan produkcije, smanjiti na 8s nakon real-world mjerenja

var winner = await Task.WhenAny(dispatchTask, timeoutTask);

if (winner == dispatchTask && dispatchTask.Result.Success)
{
    return Ok(new { aktId = dispatchTask.Result.AktId, status = "received_in_registry" });
}
else
{
    // Ili timeout, ili pokušaj nije uspio
    return Accepted(new { status = "processing", outboxId });
}
```

**Korak 3: Background dispatcher (uvijek radi)**

`OutboxDispatcher` (BackgroundService) svakih 5 sekundi:

```sql
SELECT * FROM integration_outbox
  WHERE status = 'pending'
    AND next_attempt_at <= now()
  ORDER BY created_at
  LIMIT 10
  FOR UPDATE SKIP LOCKED;
```

Za svaki red:
1. UPDATE status = 'processing'
2. Pozovi `ILocalDbAktWriter.WriteAktAsync` s `IdempotencyKey = request_id`
3. Na rezultat:

**Success ili IsDuplicate=true (vidi 9.4):**

```sql
BEGIN;
INSERT INTO seup_akt_mappings (request_id, akt_id, ...)
  VALUES (:request_id, :akt_id, ...)
  ON CONFLICT (request_id) DO NOTHING;  -- belt + braces

UPDATE requests SET status = 'received_in_registry' WHERE id = :request_id;
INSERT INTO request_status_history (...);
UPDATE integration_outbox SET status = 'done', processed_at = now() WHERE id = :outbox_id;
INSERT INTO notifications (...);             -- za građanina + officera
COMMIT;
```

**Fail (transient):**

```sql
UPDATE integration_outbox SET
  status = 'pending',
  attempts = attempts + 1,
  last_error = :error,
  next_attempt_at = now() + (POWER(5, attempts) || ' seconds')::interval
  -- 5s, 25s, 2m, 10m, 52m
WHERE id = :outbox_id;
```

Nakon 5 pokušaja → `dead_letter` + alert adminu (in-app + email officeru i adminu).

### 9.3 Idempotencija — 4 sloja zaštite

**Sloj 1 — UNIQUE u outboxu**

`integration_outbox.idempotency_key UNIQUE` garantira da postoji najviše jedan outbox red po `request_id`. Ako officer dvaput klikne, drugi INSERT padne na constraint, drugi API call vraća 409.

**Sloj 2 — Status check pri prihvaćanju**

API endpoint odbija accept ako status nije `submitted`. Drugi klik vidi `processing_registry` ili `received_in_registry` i dobije 409.

**Sloj 3 — UNIQUE u portal mappingu**

`seup_akt_mappings.request_id UNIQUE` + `ON CONFLICT DO NOTHING`. Čak i ako dispatcher kojim čudom dvaput obradi isti red, mapping se ne duplira.

**Sloj 4 — Idempotencija u SEUP-u (ključno!)**

Adapter `LocalDbAktWriter` na početku transakcije:

```sql
-- Provjeri postoji li već akt s ovim portal_request_id
SELECT AktID FROM tblAkti WHERE PortalRequestID = @requestId;
-- ako postoji: vrati AktWriteResult { Success=true, AktId=..., IsDuplicate=true }
-- ako ne: nastavi s INSERT-ima
```

**Ovo zahtijeva da `tblAkti` ima dodatan stupac `PortalRequestID uniqueidentifier` s UNIQUE indexom (filter na NOT NULL).** Čak i ako sam SQL kod prođe duplo zbog bug-a, baza odbije drugi INSERT.

⚠️ **Akcija prije produkcije (nepregovorljivo):** dogovoriti s SEUP DBA-om dodavanje stupca `PortalRequestID` na `tblAkti` + filtered unique index. Ovo je *jedina* shema-modifikacija SEUP-a koju portal traži. Bez nje, sloj 4 ne postoji i sva ostala tri sloja imaju teorijske rupe.

### 9.4 Što radimo s "duplikat" odgovorom

Ako adapter vrati `IsDuplicate=true`:
- To znači: SEUP već ima akt za ovaj zahtjev (prethodni dispatcher je uspio, ali je naš commit u portalu padao)
- Adapter čita postojeći `AktID` iz SEUP-a po `PortalRequestID` i vraća ga
- Dispatcher tretira identično kao success — upisuje mapping, mijenja status, šalje notifikaciju
- Ovo je samoizlječivo ponašanje

### 9.5 Što officer vidi

| Stanje | Officer vidi | Građanin vidi |
|---|---|---|
| Klik "Zaprimi", sync uspio < 12s | Toast "Zaprimljeno pod brojem akta NNNN", redirect na inbox | `received_in_registry` + email |
| Klik "Zaprimi", sync timeout | Modal "Zaprimanje u tijeku, možete nastaviti raditi", redirect na inbox, indikator "U procesiranju" | I dalje `submitted` (interni `processing_registry` je sakriven) |
| Async kasnije uspio | In-app notifikacija "Zahtjev ZHT-... uspješno zaprimljen pod brojem akta NNNN" | `received_in_registry` + email |
| Dead-letter nakon 5 pokušaja | Crveni banner u inboxu + in-app + email; gumb "Pokušaj ponovno" | I dalje `submitted` |
| "Pokušaj ponovno" | Outbox iz `dead_letter` u `pending`, attempts reset | — |

**Dnevni admin alert:** `OutboxStaleAlerter` worker (vidi sekciju 15) jednom dnevno šalje admin korisnicima tenanta sumarni alert za sve `dead_letter` redove koji još nisu resolved. Ovo postoji jer pojedinačni alert iz prethodne tablice ide officeru koji je inicirao zaprimanje, a on može biti odsutan, na godišnjem ili više ne raditi za JLS — bez ovog dnevnog sweepa dead-letter zahtjevi mogu tiho istrunuti i građanin nikad ne dobije akt. Admin je odgovoran za follow-up.

---

## 10. API endpointi

Base URL: `/api/v1`. Auth: `Authorization: Bearer <jwt>`. Tenant: `X-Tenant-Code` header + JWT claim cross-check. Errors: RFC 7807.

Role: **C** = građanin, **O** = officer, **A** = admin, **Pub** = javno.

### 10.1 Auth

| Metoda | URL | Role |
|---|---|---|
| POST | `/auth/register` | Pub |
| POST | `/auth/verify-email` | Pub |
| POST | `/auth/login` | Pub |
| POST | `/auth/refresh` | Pub |
| POST | `/auth/logout` | C/O/A |
| POST | `/auth/password/forgot` | Pub |
| POST | `/auth/password/reset` | Pub |
| GET | `/auth/me` | C/O/A |

### 10.2 Financije (građanin)

| Metoda | URL | Role |
|---|---|---|
| GET | `/finance/cards` | C |
| GET | `/finance/cards/{code}/open-items` | C |
| GET | `/finance/open-items/{id}/hub3` | C |
| POST | `/finance/refresh` | C (rate-limited) |

OIB iz JWT claim-a, nikad iz URL-a.

### 10.3 Zahtjevi (građanin)

| Metoda | URL | Role | Bilješka |
|---|---|---|---|
| GET | `/request-types` | C | samo aktivni |
| GET | `/request-types/{code}` | C | "preflight" detalj za Ekran 2 |
| GET | `/request-types/{id}/schema` | C | shema za render forme |
| GET | `/requests` | C | samo moji, paginirano |
| POST | `/requests` | C | kreira `draft` + snapshot sheme |
| GET | `/requests/{id}` | C (vlasnik) | |
| PATCH | `/requests/{id}` | C (vlasnik, samo `draft`) | auto-save, zahtijeva `If-Match` ETag |
| DELETE | `/requests/{id}` | C (vlasnik, samo `draft`) | |
| POST | `/requests/{id}/attachments` | C | multipart |
| DELETE | `/requests/{id}/attachments/{attId}` | C | samo prije submita |
| POST | `/requests/{id}/submit` | C | |
| GET | `/requests/{id}/attachments/{attId}/download` | C (vlasnik) | |
| GET | `/requests/{id}/history` | C (vlasnik) | |

### 10.4 Back-office (officer)

| Metoda | URL | Role |
|---|---|---|
| GET | `/office/inbox` | O |
| GET | `/office/inbox/unread-count` | O |
| GET | `/office/requests/{id}` | O |
| GET | `/office/requests/{id}/attachments/{attId}/preview` | O |
| GET | `/office/requests/{id}/attachments/{attId}/download` | O |
| POST | `/office/requests/{id}/accept` | O |
| POST | `/office/requests/{id}/reject` | O |
| POST | `/office/requests/{id}/retry-accept` | O |
| GET | `/office/rejection-reasons` | O |

Inbox query parametri: `tab`, `requestTypeId`, `search`, `dateFrom`, `dateTo`, `sort`, `page`, `size`.

### 10.5 Admin — vrste zahtjeva

| Metoda | URL | Role |
|---|---|---|
| GET | `/admin/request-types` | A |
| POST | `/admin/request-types` | A |
| GET | `/admin/request-types/{id}` | A |
| PUT | `/admin/request-types/{id}` | A |
| DELETE | `/admin/request-types/{id}` | A |
| POST | `/admin/request-types/{id}/activate` | A |
| POST | `/admin/request-types/{id}/deactivate` | A |
| POST | `/admin/request-types/{id}/duplicate` | A |
| GET | `/admin/request-types/{id}/usage` | A |

### 10.6 Admin — korisnici

| Metoda | URL | Role |
|---|---|---|
| GET | `/admin/users` | A |
| POST | `/admin/users/officers` | A |
| PATCH | `/admin/users/{id}` | A |

### 10.7 Notifikacije

| Metoda | URL | Role |
|---|---|---|
| GET | `/notifications` | C/O/A |
| POST | `/notifications/{id}/read` | C/O/A |
| POST | `/notifications/read-all` | C/O/A |

### 10.8 Health & meta

| Metoda | URL | Role |
|---|---|---|
| GET | `/health/live` | Pub |
| GET | `/health/ready` | Pub |
| GET | `/meta/tenant` | C/O/A |

---

## 11. Admin modul (vrste zahtjeva)

### 11.1 Mentalni model

Admin nije developer. Razmišlja: "Treba mi obrazac za X, građanin upisuje Y, prilaže Z." UI mora to pretvoriti u `request_types` zapis bez da admin vidi JSON, regex, ili "field type".

### 11.2 Ekrani

**Ekran A — Popis vrsta zahtjeva**
- Tablica (desktop) / kartice (mobile)
- Stupci: naziv, status badge (Aktivno/Neaktivno/Arhivirano), broj polja, broj privitaka, datum izmjene, akcije (Uredi, Deaktiviraj, Dupliciraj, Obriši)
- Filter: aktivni/svi/arhivirani, search po nazivu
- Gumb gore desno: "Nova vrsta zahtjeva"

**Ekran B — Uređivanje vrste**

Tabovi (akordeon na mobilu): **Osnovno**, **Polja**, **Privitci**, **Preview**.

**Tab 1 — Osnovno:**
- Naziv (hr) — required
- Naziv (en) — optional
- Interni kod — auto-generiran iz naziva, editabilan, slug format
- Opis (hr) — textarea, plain text
- Opis (en) — optional
- Status: Aktivno / Neaktivno (toggle)
- **Procijenjeno trajanje obrade (dana)** — number, optional, placeholder "Ostavite prazno za default tenanta ({N})"
- Redoslijed prikaza — number

**Tab 2 — Polja forme:**
- Lista polja, drag-drop za redoslijed
- Gumb "Dodaj polje" → modal s:
  - Tip polja (dropdown s ikonama)
  - Naziv (hr/en)
  - Pomoćni tekst (hr/en)
  - Obvezno (checkbox)
  - Napredne postavke (collapsible): min/max, regex, opcije za select, ...
  - Interni ključ (auto, editabilan)
- Polja se buffer-iraju u React state-u, šalju se tek na "Spremi vrstu"

**Tab 3 — Privitci:**
- Lista privitaka, drag-drop
- Modal: naziv (hr/en), opis (hr/en), obvezan, dozvoljeni formati (multi-select), max veličina (dropdown 2/5/10/25 MB), interni ključ

**Preview tab:**
- Renderira `DynamicFormRenderer` s `mode="preview"`
- Submit i upload disabled (dummy file picker)
- Sharing istog rendera kao građanski flow (jedna komponenta, dva moda)

### 11.3 Verzioniranje

Pri "Spremi vrstu zahtjeva":

**Slučaj 1 — Nova vrsta:** trivijalno, version = 1.

**Slučaj 2 — Uređivanje, nema podnesenih zahtjeva:** UPDATE in-place, version ostaje.

**Slučaj 3 — Uređivanje, ima podnesenih zahtjeva:**
- Detektira se **strukturna promjena**
- Modal: "Ova vrsta ima X podnesenih zahtjeva. Vaše promjene stvaraju novu verziju obrasca (vN+1). Postojeći zahtjevi ostaju pod vN. Svi nacrti pod vN bit će zaključani 30 dana, nakon čega se brišu."
- Admin mora eksplicitno potvrditi
- Backend: novi `request_types_version`, stara verzija ostaje za referencu

**Što je strukturna promjena (forsira version bump):**
- Dodano/obrisano polje ili privitak
- Promijenjen `field_type`
- `is_required` iz `false` u `true`
- Promijenjen `field_key` ili `attachment_key`
- Promijenjen `allowed_mime_types` (sužen)
- Promijenjen `max_size_bytes` (smanjen)

**Što NIJE strukturna promjena:**
- Promjena `label_i18n`, `help_text_i18n`, `description_i18n`
- Promjena `sort_order`
- `is_required` iz `true` u `false`
- `validation_rules` (osim required)
- `max_size_bytes` povećan
- `allowed_mime_types` proširen

### 11.4 Brisanje

- Soft delete (`is_archived = true`)
- Blokira se ako postoje aktivni `draft` ili `submitted` zahtjevi → "Deaktivirajte umjesto brisanja"
- Deaktivacija (`is_active = false`) skriva vrstu od građana, ali postojeći zahtjevi i dalje rade

### 11.5 Dupliciranje

Gumb "Dupliciraj" → kreira kopiju s imenom "Kopija od X", statusom Neaktivno, version=1.

### 11.6 Što NE radimo u MVP-u

- Conditional logic ("ako A=da pokaži B")
- Grupiranje polja u sekcije
- Ponovljiva polja
- Računata polja
- File upload kao polje (privitci su zaseban koncept)

### 11.7 Audit

Svaka admin akcija → `audit_log`:
- `request_type.created`
- `request_type.updated` (before/after)
- `request_type.version_bumped`
- `request_type.activated` / `deactivated`
- `request_type.archived`
- `request_type.duplicated`

---

## 12. Građanski flow

### 12.1 Mentalni model

Mobitel u redu za kavu. Minimalno koraka, jasan primary gumb sticky na dnu. Auto-save kako se ne bi izgubili podaci.

### 12.2 Ekrani

**Nulti korak — Zahtjev za elektronsku komunikaciju**

Prije nego građanin može podnositi ikakve zahtjeve, mora biti povezan sa SEUP-om (imati `seup_subject_id`). Flow:

1. Građanin se registrira na portalu (email + lozinka)
2. Verificira email
3. Vidi poruku: "Za korištenje portala morate podnijeti zahtjev za elektronsku komunikaciju"
4. Podnosi posebnu vrstu zahtjeva "Zahtjev za elektronsku komunikaciju" — osnovni podaci (ime, prezime, OIB, adresa)
5. Službenik u back-officeu vidi zahtjev, provjerava u SEUP-u postoji li subjekt s tim OIB-om:
   - Ako postoji → uzima SubjektID
   - Ako ne postoji → ručno kreira subjekta u SEUP-u → dobiva SubjektID
6. Službenik u portalu klikne "Odobri e-komunikaciju" i unese SubjektID
7. Portal sprema `seup_subject_id` na `users` tablici
8. Od sad građanin može podnositi ostale zahtjeve

**Implementacija:**
- "Zahtjev za elektronsku komunikaciju" je obična vrsta zahtjeva koju admin kreira kroz admin modul, ali s oznakom `is_ecommunication_request bool` na `request_types` tablici
- Accept flow za ovu vrstu NE piše u tblAkti — umjesto toga ažurira `users.seup_subject_id`
- Dok građanin nema `seup_subject_id`, gumb "Započni zahtjev" na svim ostalim vrstama je disabled s porukom "Prvo podnesite zahtjev za elektronsku komunikaciju"
- Ekran 2 (preflight) za e-komunikaciju nema preflight — odmah forma (jer je preduvjet za sve ostalo)

**Ekran 1 — Odabir vrste zahtjeva**
- URL: `/requests/new`
- Flat lista aktivnih vrsta (kategorije u fazi 2)
- Search bar
- Klik na vrstu → Ekran 2

**Ekran 2 — Preflight (pregled vrste)**
- URL: `/requests/new/{requestTypeCode}`
- **Ne** prikazuje formu odmah — sprječava napuštene draftove
- Sadržaj:
  - Naziv (h1)
  - Puni opis
  - Sekcija "Što ćete trebati": lista obveznih polja (samo nazivi) + obvezni privitci (naziv + opis + format + max veličina) + opcionalni privitci
  - Sekcija "Što se događa nakon": "Vaš zahtjev pregleda službena osoba u roku od X radnih dana." (X iz `request_types.estimated_processing_days` → fallback `tenants.settings.default_processing_days` → 5)
- Sticky gumb: **"Započni zahtjev"** → POST `/requests` → kreira draft → redirect na Ekran 3
  - **Disabled ako građanin nema verificiran email** (`email_verified_at IS NULL`). Umjesto klika prikazuje se inline upozorenje: "Za podnošenje zahtjeva morate prvo verificirati email adresu." s linkom "Pošalji ponovno verifikacijski email". Ovo sprječava anonimno spamanje pisarnice i osigurava da građanin može primiti potvrdu i status notifikacije.

**Ekran 3 — Forma**
- URL: `/requests/{id}/edit`
- Sticky header s nazivom vrste i status auto-save ("Sprema se..." / "Spremljeno u 14:32")
- Tijelo: dinamički renderirana polja iz `form_schema_snapshot`
- Sekcija "Privitci" odvojena horizontalnom linijom
- Sticky footer: "Spremi i nastavi kasnije" (secondary) + "Dalje" (primary)
- **Jedna duga forma**, ne wizard (collapsible sekcije ako > 15 polja, ali tek kad treba)
- Auto-save: PATCH `/requests/{id}` debounced 3s, šalje samo `formData`, `If-Match` ETag, na 409 toast "uređeno u drugom tabu"
- Validacija inline na blur + final na "Dalje"
- Upload privitka: native file picker, klijentska validacija MIME/veličine, multipart POST, progress bar, chunked > 5 MB, retry na fail, thumbnail/ikona + "Zamijeni"/"Ukloni"

**Ekran 4 — Potvrda prije slanja**
- URL: `/requests/{id}/review`
- Sažetak: vrsta + datum + svi odgovori (sa "Uredi" linkovima) + privitci
- Checkbox: "Potvrđujem da su navedeni podaci istiniti i točni" (obvezan)
- Tekst: "Nakon slanja zahtjev više ne možete uređivati."
- Sticky footer: "Natrag" + **"Pošalji zahtjev"** (disabled dok checkbox nije čekiran)
- Klik "Pošalji" → POST `/requests/{id}/submit` → loading → Ekran 5

**Ekran 5 — Potvrda slanja**
- URL: `/requests/{id}/submitted`
- Velika ✓ ikona
- "Vaš referentni broj: **ZHT-2026-000123**" (krupno, copy gumb)
- "Poslali smo vam potvrdu na email"
- Tekst: "Službena osoba pregledat će vaš zahtjev. Ovo obično traje 1–{N} radnih dana."
- Gumbi: "Pregled mojih zahtjeva" + "Početna"

**Ekran 6 — Moji zahtjevi (popis)**
- URL: `/requests`
- Filter chips: Svi / Nacrti / U obradi / Zaprimljeni / Odbijeni
- Kartica: naziv vrste, referentni broj, status badge, datum, "Akt: NNN" ako `received_in_registry`
- Status boje: draft=siva, submitted=plava, received_in_registry=zelena, rejected_by_officer=burgundy
- Empty state s pozivom na akciju

**Ekran 7 — Detalj zahtjeva**

Sadržaj ovisi o statusu:

| Status | Sadržaj |
|---|---|
| `draft` | Banner "Ovo je nacrt" + sažetak + "Nastavi uređivanje" / "Obriši nacrt" |
| `submitted` | Status badge "U obradi" + sažetak read-only + privitci s download + povijest |
| `received_in_registry` | Zeleni banner s **brojem akta** + sažetak + privitci + povijest + tekst "Daljnji tijek vodi se u SEUP-u, kontakt: ..." |
| `rejected_by_officer` | Burgundy banner s **razlogom iz kataloga** + sažetak + privitci (i dalje download) + tekst "Možete podnijeti novi zahtjev" |

**Bez** internal note, **bez** imena službenice, **bez** AktID-a u stanjima prije `received_in_registry`.

### 12.3 Edge case-ovi

- **Brisanje drafta s privitcima** → DELETE drafta poziva `IAttachmentStorage.DeleteAsync` za svaki privitak (background)
- **Dva taba** → ETag concurrency, drugi tab dobije 409 + toast
- **Session timeout** → axios interceptor silent refresh, fallback `sessionStorage` snapshot + redirect na login + restore nakon
- **Vrsta deaktivirana tijekom drafta** → draft ostaje uređiv, novi se ne mogu kreirati za deaktiviranu vrstu
- **Spam zaštita** → rate limit max 5 zahtjeva dnevno per građanin

### 12.4 Što građanin NE vidi nikad

- Internal note službenice
- Ime službenice koja je zaprimila/odbila
- AktID prije `received_in_registry`
- Druge zahtjeve (RLS forsira)
- Outbox `processing_registry` stanje (vidi `submitted`)

---

## 13. Back-office (službenica)

### 13.1 Mentalni model

Profesionalni korisnik za stolom. Desktop-first, mobile responsive. Brzina, dense layout, jasne odluke.

### 13.2 Ekrani

**Ekran A — Inbox**
- URL: `/office/inbox`
- Tabovi: **Na čekanju** (default, `submitted`), **Zaprimljeni**, **Odbijeni**, **Svi**
- Tablica (desktop) / kartice (mobile)
- Stupci: status indikator (🔵 Novo / ⚪ Pregledano), vrsta zahtjeva, podnositelj, OIB, referentni broj, broj privitaka, vrijeme od podnošenja
- "Novo" vs "Pregledano" stanje:
  - Novo = `viewed_first_at IS NULL`, prikazan bold s plavom točkom
  - Pregledano = `viewed_first_at IS NOT NULL`, normal s sivom točkom
  - Označava se automatski na prvom otvaranju Ekrana B
- Filtri (slide-out): vrsta zahtjeva, datum range, OIB/ref broj, sort
- Search bar: instant search po OIB/ime/ref broj/akt ID
- Default sort: najstariji prvi (FIFO)
- Pagination: 25/50/100, default 25
- Real-time refresh: tihi toast "1 novi zahtjev (osvježi)" (ne auto-refresh)

**Ekran B — Detalj zahtjeva**
- URL: `/office/requests/{id}`
- Auto-marks `viewed_first_at` na prvom otvaranju (audit + UI stanje)
- Dvostupčani layout (desktop), jednostupčani (mobile)
- **Lijevo (60%):** vrsta zahtjeva, podaci iz forme (read-only), privitci s preview/download
- **Desno (40%):** podnositelj (ime, OIB, email, telefon, adresa), metapodaci (podnesen, pregledan), povijest statusa
- **Sticky footer:** "Odbij" (burgundy outline) + "Zaprimi u pisarnicu" (zeleni primary), razmaknuti
- **Privitci:**
  - 👁 = inline preview (PDF.js za PDF, native `<img>` za slike) — ne stvara lokalnu kopiju (GDPR plus)
  - ⬇ = download (audit log)
  - DOCX preview = faza 2

**"Zaprimi u pisarnicu" modal:**
```
Zahtjev će biti zaprimljen u SEUP kao novi akt. Akcija je nepovratna.
Podnositelj: Ana Anić (12345678901)
Vrsta: Oslobađanje od komunalne naknade
[Odustani] [Zaprimi]
```

Klik "Zaprimi":
- POST `/office/requests/{id}/accept`
- 12s timeout (prvi tjedan), pa 8s
- Sync uspjeh: toast "Zaprimljeno pod brojem akta NNNN", redirect inbox
- Async: modal "Zaprimanje u tijeku, možete nastaviti raditi", redirect inbox, "U procesiranju" indikator
- Dead-letter: žuti banner u inboxu + gumb "Pokušaj ponovno"

**"Odbij" modal:**
```
Razlog odbijanja: [▼ dropdown iz kataloga]
Interna napomena (vidi samo JLS): [textarea]
⚠ Podnositelj će biti obaviješten emailom o odbijanju s navedenim razlogom.
[Odustani] [Odbij]
```

- Razlog obvezan (dropdown)
- Interna napomena obvezna samo ako razlog = "Ostalo"
- Sinkrono (samo portal DB), instant
- Status → `rejected_by_officer`, povijest, in-app + email građaninu

### 13.3 Razlozi odbijanja (katalog)

```
inappropriate_content   "Neprimjereni sadržaj"
out_of_jurisdiction     "Nije u nadležnosti ove JLS"
duplicate               "Ponavljajući zahtjev"
not_serious             "Očito neozbiljan zahtjev"
other                   "Ostalo" (zahtijeva internal note)
```

i18n-aware, served preko `GET /office/rejection-reasons`.

### 13.4 Što službenica NE može

- Uređivati podatke ni privitke (read-only)
- Brisati zahtjev (samo "Odbij")
- Mijenjati status proizvoljno (samo dvije akcije)
- Vidjeti druge tenante (RLS)
- Vidjeti administrativne ekrane (osim ako ima i admin role)
- Poništiti vlastitu odluku — ako greška, kontakt s adminom (audit log)

### 13.5 Što vide admin vs officer

| Role | Inbox | Detalj | Akcije | Admin moduli |
|---|---|---|---|---|
| officer | ✓ | ✓ | accept/reject | — |
| admin | — | — | — | request types, users, audit |
| officer + admin | ✓ | ✓ | accept/reject | sve |

Nema "supervisor" role u MVP-u.

### 13.6 Notifikacije za officera

In-app:
- Novi zahtjev podnesen (toast + sidebar badge)
- Outbox dead-letter (vlastiti zahtjev)
- Outbox uspjeh nakon async čekanja

Email **ne** dobiva za nove zahtjeve. Dnevni digest = faza 2.

### 13.7 Audit log integracija

- `request.viewed` (prvi put)
- `request.accepted` (with before/after)
- `request.rejected` (with reason + internal note)
- `attachment.downloaded` (GDPR-relevant)
- `attachment.previewed` (opcionalno)

### 13.8 Performance

- Inbox query: indeks `(tenant_id, status, submitted_at DESC)`
- Detalj: 3 round-tripa (request, attachments, history) ili 1 spojeni u CQRS handleru
- Privitci: stream, never fully load in memory, range request support za PDF.js
- Sidebar badge: cache 30s po tenantu

### 13.9 UI komponente za izgraditi

- `OfficerLayout` (sidebar + top bar)
- `InboxTable` (sortable, filterable, virtualized)
- `InboxFilters` (slide-out)
- `RequestDetailLayout` (dvostupčani, sticky header/footer)
- `DynamicFormReadonlyRenderer` (mode prop na shared rendereru)
- `AttachmentList` + `AttachmentPreviewModal` (PDF.js + img)
- `AcceptRequestModal` (s loading + async fallback)
- `RejectRequestModal` (s validacijom razloga)
- `StatusBadge` (shared s građanskim)
- `HistoryTimeline`
- `OutboxStatusIndicator`

---

## 14. Notifikacije

### 14.1 Kanali

In-app (uvijek), Email (po definiciji događaja), SMS (faza 2).

### 14.2 Matrica događaja

**Građanin:**

| Događaj | In-app | Email |
|---|---|---|
| Zahtjev podnesen | ✓ | ✓ |
| Zahtjev zaprimljen u pisarnicu | ✓ | ✓ (s brojem akta) |
| Zahtjev odbijen | ✓ | ✓ (s razlogom) |
| Auto-save | ✗ | ✗ |
| Draft uskoro istječe (7d) | ✓ | ✓ |
| Draft zastario (version bump) | ✓ | ✗ |

**Officer:**

| Događaj | In-app | Email |
|---|---|---|
| Novi `submitted` zahtjev | ✓ | ✗ |
| Outbox dead-letter (vlastiti) | ✓ | ✓ |
| Outbox async uspjeh | ✓ | ✗ |

### 14.3 Email šabloni

- `Portal.Infrastructure.Email/Templates/*.cshtml` (Razor)
- Per-tenant override moguć preko `tenants.settings`
- i18n-aware (`hr`, `en`)

⚠️ Email provider odluka — vidi sekciju 21.

---

## 15. Background workeri

`BackgroundService` registrirani u `Portal.Api/Program.cs`:

| Worker | Interval | Što radi |
|---|---|---|
| `OutboxDispatcher` | 5s | Procesira `pending` outbox redove (vidi sekciju 9) |
| `OutboxStaleAlerter` | 24h | Skenira `integration_outbox` za redove u stanju `dead_letter` koji nisu još riješeni. Šalje (in-app + email) admin korisnicima tenanta dnevni alert "Imate N zahtjeva koji nisu uspjeli biti zaprimljeni u SEUP". Sprječava da dead-letter redovi tiho istrunu jer officer koji je inicirao zaprimanje možda više nije aktivan ili je previdio pojedinačni alert. Worker dodatno bilježi metriku za monitoring (broj dead_letter po tenantu po danu). |
| `EmailDispatcher` | 5s | Procesira `pending` `notification_deliveries` s `channel='email'` |
| `DraftCleanupWorker` | 24h (noću) | 1) Briše drafttove s `expires_at <= now`. 2) Šalje upozorenja za draftove koji ističu za 7 dana. Reset `expires_at` se ne radi ovdje (radi se na PATCH-u) |
| `FinanceCacheCleanup` | 1h | Briše expired `finance_snapshots` |
| `RefreshTokenCleanup` | 24h | Briše expired refresh tokene |

Sve workere mora biti moguće uključiti/isključiti preko config flag-a (testiranje).

---

## 16. Frontend napomene

- **Mobile-first** za građanski, **desktop-first** za back-office (admin + office)
- Breakpointi: `sm:640 md:768 lg:1024 xl:1280`
- Građanski dizajn ide od 360px širine
- `<meta name="viewport" content="width=device-width, initial-scale=1">` bez `maximum-scale`
- Font base 16px minimum, touch targets ≥ 44px
- i18n ključevi u `web/src/i18n/hr.json` (plus `en.json` placeholder)
- **Hardkodirani hrvatski tekst u komponenti = bug** (PR review)
- Forme: react-hook-form + zod, dinamička schema iz `request_type_fields`
- Auth: access token u memoriji, refresh token u httpOnly SameSite=strict cookie, axios interceptor za auto-refresh
- State: TanStack Query za server state, zustand/context za UI state, **bez Reduxa**
- Routing: react-router v6, lazy loaded feature moduli
- **Shared komponenta:** `DynamicFormRenderer` s `mode="edit" | "readonly" | "preview"` za sve tri uloge
- **UI sustav (boje, tipografija, spacing, komponente):** vidi playbook sekciju **"UI sustav"**. CLAUDE.md ne duplicira dizajn tokene — tamo žive Tailwind config, paleta, font skala, button varijante, badge stilovi i ostali shared dizajn primitivi koji se koriste i u drugim projektima portfelja. Ako playbook i CLAUDE.md proturječe oko UI-ja, playbook ima prednost.

---

## 17. Sigurnost

- **Lozinke:** BCrypt work factor 12
- **JWT:** HS256 (dev) / RS256 (prod), access 15 min, refresh 14 dana, rotacija na svakom refreshu
- **CORS:** whitelist iz `tenants.settings`
- **Rate limiting:**
  - Auth endpointi: per-IP
  - `POST /requests`: per-user 5/dan
  - `POST /finance/refresh`: per-user 10/min
- **CSRF:** N/A (Bearer auth, refresh cookie SameSite=strict)
- **Privitci:**
  - Server-side MIME provjera (magic bytes, ne trust na Content-Type)
  - `IAttachmentScanner` interface — stub za sad, ClamAV kandidat
- **SQL injection:** EF Core parametrizirano (PostgreSQL), Dapper s parametrima (SQL Server). Nikad string concat.
- **PII u logovima:** Serilog enricher za maskiranje OIB-a. Nikad email/ime/punu adresu bez maskiranja.
- **Audit log za privitke ne smije sadržavati `original_filename`.** Imena fajlova često sadrže PII (npr. `Osobna_iskaznica_Marko_Maric_12345678901.pdf`). Audit log redovi za `attachment.downloaded` i `attachment.previewed` pamte samo `attachment_id`, `request_id`, `user_id`, `ip`, `user_agent`. Tko želi znati koji je fajl bio downloaded → resolve preko `attachment_id` u `request_attachments`, što je pristup koji i sam ide kroz audit i RLS.
- **GDPR:**
  - Građanin ima pravo na export i brisanje računa
  - Brisanje hard ako nema `received_in_registry` zahtjeva
  - Brisanje soft ako postoje (ne možemo izbrisati podatke iz SEUP-a)
  - PDF.js inline preview u back-officeu = manje kopija osjetljivih dokumenata na laptopima službenica

---

## 18. Testiranje

| Razina | Alat | Pokriva |
|---|---|---|
| Unit | xUnit + FluentAssertions | Domain, Application (bez baza) |
| Integration | Testcontainers PostgreSQL + xUnit | Api sloj, real DB, mock `ILocalDbAktWriter` |
| LocalDb adapter | Testcontainers MSSQL ili dedicated dev DB | `LocalDbAktWriter`, `LocalDbFinanceReader` (odvojeno od main CI ako sporo) |
| Architecture | NetArchTest | Pravila ovisnosti, `Microsoft.Data.SqlClient` zatvoren u `LocalDb` projektu |
| E2E (kasnije) | Playwright | Smoke testovi na docker-compose |

Coverage cilj: Domain 90%+, Application 80%+, Infrastructure 60%+.

**Obvezni integracijski testovi za outbox/idempotenciju (sekcija 9):**
- Dvostruki klik "Zaprimi" → samo jedan akt
- Sync timeout → async fallback uspije, građanin vidi `received_in_registry`
- Dispatcher vraća `IsDuplicate=true` → mapping se kreira jednom, status updatea jednom
- Dead-letter → "Pokušaj ponovno" vraća u `pending`
- Concurrent dispatcheri (FOR UPDATE SKIP LOCKED) → ne procesiraju isti red dvaput

---

## 19. Deployment

- **Dev:** `docker-compose up` → api + web + postgres + mailhog. SQL Server ručno (Express ili Testcontainers).
- **Pilot prod:** ⚠️ čeka odluku (sekcija 21)
- **Migracije:** EF Core migrations preko `MigrationRunner` IHostedService na startupu
- **Secrets:** environment varijable (prod), user secrets (dev). Nikad u `appsettings.json`.

### 19.1 Backup prije prod deploya (obvezno, playbook v2.3)

`MigrationRunner` u prod modu **odbija krenuti** ako nema valjanog backupa portal baze u zadnjih 60 minuta. Provjera:

- Tablica `deployment_backups (id, taken_at, size_bytes, location, verified_at)` u portal bazi.
- Backup pipeline (vanjski cron ili CI step prije deploya) upisuje red nakon uspješnog `pg_dump` + verifikacije restore-a na staging instancu.
- `MigrationRunner` na startupu: `SELECT max(taken_at) FROM deployment_backups WHERE verified_at IS NOT NULL`. Ako `now() - max > 60 min` → log error + `Environment.Exit(1)`.
- Override za hitne slučajeve: env var `PORTAL_SKIP_BACKUP_CHECK=true` + obvezni audit log red. Koristi se samo uz pisanu dozvolu admina pilota.

Razlog: bilo koja migracija je potencijalno destruktivna (vidi sekciju 20 pravilo 11). Backup je posljednja linija obrane.

### 19.2 Bootstrap tenanta (CLI, idempotentno)

Bez ovoga pilot nije demonstrabilan — netko mora moći postojati u bazi prije nego se prvi admin može logirati. Rješenje: CLI komanda u `Portal.Api` projektu, pokretana kroz `dotnet run -- bootstrap-tenant ...` ili Docker `docker compose run --rm api bootstrap-tenant ...`.

**Komanda:**

```bash
portal bootstrap-tenant \
  --code grad-x \
  --name "Grad X" \
  --oib 12345678901 \
  --admin-email admin@grad-x.hr \
  --admin-first-name "Marija" \
  --admin-last-name "Marić" \
  --admin-temp-password "<generirani-string>" \
  [--officer-email officer@grad-x.hr ...] \
  [--default-processing-days 5]
```

**Ponašanje:**

- **Idempotentno.** Ako tenant s `code='grad-x'` već postoji → ne kreira se ponovno, samo se ispiše postojeći stanje. Ako admin s tim emailom već postoji → ne mijenja lozinku, ne kreira duplikat.
- Kreira `tenants` red s default `settings` (default_processing_days=5, mail_from praznо)
- Kreira admin korisnika s `user_type='jls_admin'`, `email_verified_at = now()` (bootstrap admin ne treba mail verifikaciju), `password_hash` od privremene lozinke
- Opcionalno kreira jednog officera (`user_type='jls_officer'`, isti tretman)
- Forsira **promjenu lozinke pri prvom loginu** (novi flag `users.must_change_password bool DEFAULT false`, set na `true` za bootstrap-kreirane korisnike)
- Sve akcije idu u `audit_log` s `user_id = NULL`, `action = 'bootstrap.tenant_created' | 'bootstrap.user_created'`, i odgovarajućim entity referencama
- Status history za sve kreirane entitete koristi `changed_by_source = 'system'`
- Ispis na stdout: tenant id, korisnici (email + privremena lozinka, samo jednom — admin to mora odmah zapisati)

**Što komanda NE radi:**
- Ne kreira vrste zahtjeva (admin to radi kroz UI nakon prvog logina)
- Ne kreira građane
- Ne šalje email (pretpostavka: admin pilota sjedi pored osobe koja pokreće komandu i prima credentials usmeno/sigurnim kanalom)

**Implementacija:** `Portal.Api/Cli/BootstrapTenantCommand.cs`, registriran kroz `System.CommandLine`. `Program.cs` detektira ako su prosljeđeni argumenti tipa `bootstrap-tenant` i izvršava CLI granu umjesto pokretanja web hosta.

**Test:**
- Integration test koji pokreće komandu dva puta uzastopno, provjerava da je rezultat isti (idempotentnost)
- Test koji provjerava da bootstrap user mora promijeniti lozinku pri prvom loginu

---

## 20. Pravila rada u repu

1. **Ne diraj SEUP shemu pretpostavkama.** Pitaj — ne pogađaj.
2. **Ne izmišljaj HUB-3 format.** Reguliran. Vidi sekciju 21.
3. **Sve što ide u SQL Server prolazi kroz `Portal.Infrastructure.LocalDb`.** Iznimke nema.
4. **Commands i queries u Application sloju, ne u Api.** Api samo HTTP → MediatR mapping.
5. **Validacija u FluentValidation, ne u handlerima.**
6. **i18n od dana 1.** Hardkodirani hrvatski tekst = bug.
7. **`tenant_id` na svakom query-ju.** RLS uhvati propust, ali bolje eksplicitno.
8. **Pojmovnik (sekcija 3) je obvezan.** Pregled u svakom PR-u.
9. **Idempotencija outboxa (sekcija 9) je sveta.** Bilo koja izmjena tog koda zahtijeva eksplicitni review s testovima.
10. **Testovi prije mergea.** CI blokira ako coverage padne ili arhitekturni testovi fail-aju.
11. **Destruktivne migracije zahtijevaju ručni review.** Bilo koja EF Core migracija koja sadrži `DROP TABLE`, `DROP COLUMN`, `DELETE FROM`, rename stupca ili tablice, suženje tipa (npr. `varchar(200) → varchar(100)`), ili promjenu nullability iz `NULL` u `NOT NULL` bez default-a — mora imati `[DestructiveMigration]` atribut na klasi i ručni review od strane drugog developera + admina pilota. CI marker za destruktivne migracije: regex check nad generiranim SQL-om u PR-u, label `migration:destructive` automatski. Bez review labela merge je blokiran. Razlog: jednom izgubljen podatak iz portala je izgubljen — backup iz sekcije 19.1 je posljednja linija, ne prva.

---

## 21. ⚠️ TODO — čeka input

Apstrahirano iza interface-a. Implementer može razvijati sve ostalo. Spaja se kad podaci stignu, bez refaktoriranja.

### 21.1 tblAkti / tblPRBiljeska / tblDatoteke INSERT stupci

**RIJEŠENO u v1.3.** Svi stupci su poznati. Vidi sekciju 8.3 za puni tok s točnim stupcima.

**Sažetak stupaca:**

```
tblAkti:
  AktID (long)              — app generira: MAX(AktID) + 1
  VrstaID (long)            — 0 = ulazni dokument (podnesak)
  Opis (string 255)         — naziv vrste + referentni broj
  DatumKreiranjaAkta (date) — danas
  DatumPrimitka (date)      — danas
  PredmetID (long)          — 0 (uvijek kod primitka)
  StvarateljID (long)       — 0 (uvijek kod primitka)
  IDSesije (long)           — NULL za sad (riješit će se s launcher integracijom)
  PortalRequestID (guid)    — request.id, za idempotenciju (stupac se dodaje ručno)

tblPRBiljeska:
  BiljeskaID (long)         — app generira: MAX(BiljeskaID) + 1
  Datum (date)              — danas
  SubjektID (long)          — iz users.seup_subject_id
  NacinBiljeske (long)      — 3000 (fiksno za portal podnesak)
  Opis (string 255)         — opis zahtjeva
  AktID (long)              — FK na tblAkti
  IDSesije (long)           — NULL za sad

tblDatoteke:
  DatotekaID (long)         — app generira: MAX(DatotekaID) + 1
  Datoteka (string 255)     — opis datoteke (label)
  DatumDatoteke (date)      — danas
  LokacijaDatoteke (str 255)— putanja iz IArchiveFileCopier
  NazivDatoteke (string 255)— originalno ime fajla
  AktID (long)              — FK na tblAkti
  RbrDatoteke (long)        — redni broj privitka (1, 2, 3...)
  IDSesije (long)           — NULL za sad
```

**Preostali TODO za ovu sekciju:**
- Točan format putanje u `LokacijaDatoteke` (apsolutna vs relativna) — čeka dogovor
- `PortalRequestID` stupac mora biti dodan ručno na tblAkti prije produkcije (vidi sekcija 9.3 sloj 4)

### 21.2 Putanja digitalne arhive (intake folder)

**Što znamo:**
- Postoji jedan "ulazni" folder iz kojeg servis razbacuje fajlove na trostruku arhivu
- Portal samo kopira u taj folder

**Što čekamo:**
- Točna putanja (mrežni share `\\server\...` ili lokalni mount)
- Naming konvencija fajlova
- Pristupna prava (account portal servisa)

**Privremeno:** `IArchiveFileCopier` interface postoji, `LocalArchiveFileCopier` piše u `/var/portal/archive-staging/` za dev.

### 21.3 HUB-3 specifikacija

**Što znamo:** PDF417 barcode s standardiziranim payload-om HUB.

**Što čekamo:** korisnik je rekao "rješit ćemo, dam upute".

**Privremeno:** endpoint `GET /finance/open-items/{id}/hub3` vraća `501 Not Implemented` s porukom "HUB-3 generator u pripremi".

### 21.4 Email provider

**Odlučeno:** Resend (resend.com). Besplatan tier do 3000 emailova/mjesec, dovoljno za pilot.

**Implementacija:** `ResendEmailSender` u `Portal.Infrastructure.Email`, implementira isti `IEmailSender` interface. NuGet paket: `Resend.net`. API ključ u environment varijabli `RESEND_API_KEY`.

**Dev:** Mailhog ostaje za lokalni razvoj (docker-compose). `NullEmailSender` za testove.

### 21.5 Deployment target pilota

**Odlučeno:** Contabo VPS (Cloud VPS 20 NVMe, 12 GB RAM, 6 CPU, 100 GB NVMe). Multi-tenant spreman — isti server služi više JLS-ova kad dođe do toga.

**Plan:** Docker Compose na Contabu za pilot. Migracija na ozbiljniji hosting (Hetzner Cloud, Azure) kad pilot preraste u produkciju s više JLS-ova. Dockerfile-ovi su cloud-agnostic — selidba traje sat, ne tjedan.

**Uvjet za produkciju:** VPS mora imati mrežni pristup do SEUP SQL Servera u JLS-u (VPN ili whitelist IP-a).

### 21.6 DPIA (Data Protection Impact Assessment)

**Što čekamo:** Legal task #9 — provedba DPIA-e prema GDPR čl. 35 prije go-live pilota. Portal obrađuje OIB, ime, adresu, kontakt, zdravstveno-socijalne podatke (ovisno o vrsti zahtjeva) i privitke koji često sadrže osjetljive osobne dokumente — to je jasna kandidatura za obvezni DPIA.

**Što DPIA mora pokriti:**
- Pravne osnove obrade po vrsti zahtjeva
- Retencija (CLAUDE.md sekcija 17 + 12.3)
- Tko ima pristup čemu (RLS, role)
- Tijek prema SEUP-u i odgovornosti voditelja obrade nakon prijenosa
- Privitci u digitalnoj arhivi (nadležnost JLS-a, ne portala)
- Postupak za zahtjeve subjekata (export, brisanje)

**Ne blokira razvoj.** Razvoj ide paralelno. DPIA mora biti **potpisana prije produkcijskog go-live-a pilota**, ne prije scaffold-a ili prije internog test deploya.

**Akcija:** Agent #0 stavlja u Legal backlog kao stavku #9 s rokom = T-14 dana od planiranog go-live datuma.

---

## 22. Faza 2 backlog (NE u MVP-u)

**Integracija:**
- Povratne informacije o statusu predmeta iz SEUP-a (polling vs trigger)
- NIAS / e-Građani login

**Komunikacija:**
- SMS notifikacije
- Push notifikacije (web push)
- Komentari između građanina i službenice unutar zahtjeva
- Dnevni email digest za officere

**Uloge:**
- LDAP/AD integracija za službenike
- Supervisor role (poništavanje odluka, povijest po službenici)

**Funkcionalnost:**
- **Dupliciraj odbijeni zahtjev** (jedan klik kopira `formData` + privitke iz `rejected_by_officer` u novi draft; storage-level copy fajlova)
- PDF preuzimanje cijelog zahtjeva za građaninovu evidenciju
- Bulk akcije u back-officeu
- Dodjela zahtjeva konkretnoj službenici (assignment)
- Tagging zahtjeva
- Export inboxa u Excel/CSV
- Anuliranje zaprimanja (zahtijeva integraciju natrag u SEUP)

**Admin:**
- Conditional logic ("ako A=da pokaži B")
- Grupiranje polja u sekcije
- Ponovljiva polja
- Računata polja
- Kategorije vrsta zahtjeva

**Drugo:**
- Više JLS-ova na istoj instanci portala
- Engleski jezik
- DOCX preview u back-officeu
- Spremanje "favorita" / čestih zahtjeva
- Kopiranje podataka iz prethodnog zahtjeva

---

**Kraj CLAUDE.md v1.3.**

Promjene u v1.3:
- Sekcija 1: dodano da je SEUP naš vlastiti proizvod
- Sekcija 6.1: `seup_subject_id` na `users` tablici
- Sekcija 6.2: `is_ecommunication_request` na `request_types` tablici
- Sekcija 8.3: kompletni stupci za tblAkti/tblPRBiljeska/tblDatoteke, AktID logika (MAX+1), IDSesije=NULL
- Sekcija 8.3: PortalRequestID startup check s graceful degradation
- Sekcija 8.4: Resend kao email adapter za produkciju
- Sekcija 12.2: "Zahtjev za e-komunikaciju" kao nulti korak (SubjektID flow)
- Sekcija 21.1: RIJEŠENO — svi SEUP stupci poznati
- Sekcija 21.4: RIJEŠENO — Resend odabran
- Sekcija 21.5: RIJEŠENO — Contabo VPS za pilot

Sljedeća verzija (v1.4) stiže kad se zatvore preostale TODO stavke (putanja arhive, HUB-3, DPIA).
