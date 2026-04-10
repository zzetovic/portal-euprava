# Backend zadaci — Sprint 1

## Auth sustav
- [x] POST /auth/register — citizen self-registration, email+password, BCrypt hash, email_verification_token
- [x] POST /auth/verify-email — token validation, set email_verified_at
- [x] POST /auth/login — credentials check, JWT access (15min) + refresh token (14d, httpOnly cookie)
- [x] POST /auth/refresh — rotate refresh token, return new access
- [x] POST /auth/logout — revoke refresh token
- [x] POST /auth/password/forgot — generate password_reset_token, enqueue email
- [x] POST /auth/password/reset — validate token, update password_hash
- [x] GET /auth/me — return current user profile from JWT claims
- [x] Tenant middleware — extract X-Tenant-Code header, cross-check with JWT claim, SET app.tenant_id for RLS
- [x] must_change_password flag — force password change on first login for bootstrap users

## Bootstrap tenant CLI (sekcija 19.2)
- [ ] BootstrapTenantCommand.cs — System.CommandLine registration in Program.cs
- [ ] Idempotentno kreiranje tenanta (code, name, oib, settings)
- [ ] Kreiranje admin usera (jls_admin, email_verified_at=now, must_change_password=true)
- [ ] Opcionalno kreiranje officer usera
- [ ] Audit log s user_id=NULL, action=bootstrap.*, changed_by_source=system
- [ ] Stdout ispis credentials (samo jednom)
- [ ] Integration test: pokretanje dva puta uzastopno = isti rezultat

## Admin — vrste zahtjeva (sekcija 11 + API 10.5)
- [ ] GET /admin/request-types — lista s filterima (active, archived, search)
- [ ] POST /admin/request-types — kreiranje nove vrste (basic + fields + attachments, version=1)
- [ ] GET /admin/request-types/{id} — detalj s poljima i privitcima
- [ ] PUT /admin/request-types/{id} — update s detekcijom strukturne promjene i version bump
- [ ] DELETE /admin/request-types/{id} — soft delete (is_archived=true), blokira ako ima aktivne zahtjeve
- [ ] POST /admin/request-types/{id}/activate
- [ ] POST /admin/request-types/{id}/deactivate
- [ ] POST /admin/request-types/{id}/duplicate — kopija s "Kopija od X", Neaktivno, version=1
- [ ] GET /admin/request-types/{id}/usage — broj draft/submitted/received/rejected zahtjeva
- [ ] FluentValidation za sve DTOeve
- [ ] Audit log za sve admin akcije (created, updated, version_bumped, activated, deactivated, archived, duplicated)

## Građanin — zahtjevi (sekcija 12 + API 10.3)
- [ ] GET /request-types — samo aktivni za tenant, i18n-aware
- [ ] GET /request-types/{code} — preflight detalj (name, description, fields summary, attachments, estimatedProcessingDays s fallback)
- [ ] GET /request-types/{id}/schema — puna shema za render forme
- [ ] POST /requests — kreira draft, snapshot sheme, reference_number (ZHT-YYYY-NNNNNN), expires_at=now+90d
- [ ] GET /requests — moji zahtjevi, paginirano, filter po statusu. CitizenRequestStatusMapper: processing_registry → submitted
- [ ] GET /requests/{id} — detalj (samo vlasnik, RLS)
- [ ] PATCH /requests/{id} — auto-save formData, If-Match ETag, sliding expires_at reset, samo draft
- [ ] DELETE /requests/{id} — samo draft, background delete privitaka iz storage-a
- [ ] POST /requests/{id}/attachments — multipart upload, server-side MIME check (magic bytes), checksum
- [ ] DELETE /requests/{id}/attachments/{attId} — samo prije submita
- [ ] POST /requests/{id}/submit — validacija svih obveznih polja + privitaka, status draft→submitted, enqueue email+in-app notifikacija
- [ ] GET /requests/{id}/attachments/{attId}/download — stream, samo vlasnik
- [ ] GET /requests/{id}/history — status history
- [ ] Email verification check — blokiraj submit ako email_verified_at IS NULL
- [ ] Rate limit: max 5 POST /requests per citizen per day

## Officer — back-office (sekcija 13 + API 10.4)
- [ ] GET /office/inbox — tab filter (pending/received/rejected/all), search, date range, sort (default oldest first), pagination 25/50/100
- [ ] GET /office/inbox/unread-count — za sidebar badge, cache 30s per tenant
- [ ] GET /office/requests/{id} — auto-mark viewed_first_at + viewed_first_by_user_id na prvom otvaranju, audit log
- [ ] GET /office/requests/{id}/attachments/{attId}/preview — stream, Content-Disposition: inline
- [ ] GET /office/requests/{id}/attachments/{attId}/download — stream, Content-Disposition: attachment, audit log (bez original_filename!)
- [ ] POST /office/requests/{id}/reject — razlog obvezan, internal_note obvezan ako razlog=other, status→rejected_by_officer, enqueue notifikacije
- [ ] GET /office/rejection-reasons — katalog razloga, i18n-aware

## Outbox + accept flow (sekcija 9 — KRITIČNO)
- [ ] POST /office/requests/{id}/accept — jedna PostgreSQL transakcija: status check FOR UPDATE, outbox insert s idempotency_key=request_id, status→processing_registry, history insert
- [ ] Sync wait s 12s timeout + async fallback (200 s aktId ili 202 s processing)
- [ ] OutboxDispatcher BackgroundService — 5s interval, FOR UPDATE SKIP LOCKED, max 10 redova
- [ ] ILocalDbAktWriter.WriteAktAsync — stub koji baca NotImplementedException (TODO sekcija 21.1)
- [ ] Dispatcher success path: seup_akt_mappings INSERT ON CONFLICT DO NOTHING, status→received_in_registry, history, notifikacije
- [ ] Dispatcher fail path: exponential backoff (5s, 25s, 2min, 10min, 52min), dead_letter nakon 5 pokušaja
- [ ] IsDuplicate=true handling — tretira kao success, čita postojeći AktID
- [ ] POST /office/requests/{id}/retry-accept — dead_letter→pending, attempts reset
- [ ] OutboxStaleAlerter worker — 24h, dead_letter alert adminu (in-app + email)

## Notifikacije (sekcija 14 + API 10.7)
- [ ] GET /notifications — paginirano, za current user
- [ ] POST /notifications/{id}/read — mark is_read, set read_at
- [ ] POST /notifications/read-all — bulk mark za current user
- [ ] EmailDispatcher BackgroundService — 5s interval, procesira pending notification_deliveries channel=email
- [ ] Razor email templates skeleton (zahtjev podnesen, zaprimljen, odbijen, draft istječe)

## Background workeri (sekcija 15)
- [ ] DraftCleanupWorker — 24h noću: briše draftove s expires_at<=now, upozorenja 7 dana prije isteka
- [ ] FinanceCacheCleanup — 1h, briše expired finance_snapshots
- [ ] RefreshTokenCleanup — 24h, briše expired refresh tokene
- [ ] Config flag za enable/disable svakog workera

## Health & meta
- [ ] GET /health/live — always 200
- [ ] GET /health/ready — provjera PostgreSQL konekcije
- [ ] GET /meta/tenant — tenant info za frontend (name, branding, settings)
