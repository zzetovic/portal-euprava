# QA zadaci — Sprint 1

## Architecture tests (PRVO — osiguravaju strukturu)
- [ ] Domain ne referencira Application ni Infrastructure ni Api
- [ ] Application ne referencira Infrastructure ni Api
- [ ] Microsoft.Data.SqlClient postoji SAMO u Portal.Infrastructure.LocalDb
- [ ] Svi CQRS handleri su u Portal.Application
- [ ] Svi FluentValidation validatori su u Portal.Application

## Unit testovi — Domain
- [ ] RequestStatus enum — svi statusi postoje uključujući processing_registry
- [ ] Request entitet — status transitions (draft→submitted OK, submitted→draft FAIL, itd.)
- [ ] ReferenceNumber format validacija (ZHT-YYYY-NNNNNN)
- [ ] OIB checksum validacija
- [ ] IBAN validacija

## Unit testovi — Application
- [ ] CitizenRequestStatusMapper — processing_registry mapira u submitted
- [ ] Strukturna promjena detekcija (version bump pravila iz sekcije 11.3)
- [ ] Draft expiration logika (90d sliding, 30d locked)

## Integration testovi — Auth
- [ ] Register + verify email + login flow
- [ ] Refresh token rotacija
- [ ] Expired token → 401
- [ ] must_change_password → forsira promjenu

## Integration testovi — Outbox/Idempotencija (KRITIČNO, sekcija 9)
- [ ] Dvostruki klik "Zaprimi" → samo jedan outbox red (UNIQUE constraint)
- [ ] Status check: accept na non-submitted → 409
- [ ] Sync timeout → 202 Accepted, dispatcher završi async
- [ ] Dispatcher IsDuplicate=true → mapping kreira jednom, status update jednom
- [ ] Dead-letter → retry-accept vraća u pending
- [ ] Concurrent dispatcheri (FOR UPDATE SKIP LOCKED) → ne procesiraju isti red dvaput
- [ ] seup_akt_mappings UNIQUE constraint — ON CONFLICT DO NOTHING

## Integration testovi — Bootstrap tenant
- [ ] Prva izvršenja kreira tenant + admin
- [ ] Drugo izvršenje ne kreira duplikat (idempotentnost)
- [ ] Bootstrap user ima must_change_password=true

## i18n provjere
- [ ] Scan svih React komponenti — nema hardkodiranog hrvatskog teksta
- [ ] Svi ključevi iz hr.json postoje u en.json (makar prazni)
- [ ] Pojmovnik provjera: nigdje "klasa", "broj akta" u krivom kontekstu, nigdje "caseNumber" u DTO-ima
