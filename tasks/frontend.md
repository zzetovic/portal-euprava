# Frontend zadaci — Sprint 1

## Shared Design System (graditi PRVO, koristi se svugdje)
- [x] Theme CSS varijable (prema playbook UI sustav sekciji): primary, secondary, accent, background, text, error, success + dark/light
- [x] Button komponenta — primary, secondary, danger, disabled varijante, min 48px na mobitelu
- [x] Input komponenta — text, email, number, textarea, select, date, checkbox. Label iznad, error state, disabled state, help text ispod
- [x] Modal komponenta — otvara se PRI VRHU ekrana (ne centar!), overlay s klik-za-zatvori, X gumb, ESC zatvara, max 90vh, okvir u accent boji
- [x] Toast/Notification komponenta — success/error/warning, gornji desni kut desktop, vrh ekrana mobile, auto-dismiss 5s
- [x] StatusBadge komponenta — draft=siva, submitted=plava, received_in_registry=zelena, rejected_by_officer=burgundy
- [x] LoadingSkeleton komponenta — pulsing gray rectangles umjesto spinnera
- [x] EmptyState komponenta — ilustracija + poruka + CTA gumb
- [x] Viewport meta tag + mobile-first CSS reset (box-sizing, overflow-x hidden, img max-width)

## Auth stranice
- [x] Login stranica — email + password, error handling, redirect na dashboard
- [x] Register stranica — citizen self-registration, email + password + ime + prezime + OIB (opcionalno) + telefon (opcionalno)
- [x] Verify email stranica — prima token iz URL-a, poziva API, success/error state
- [x] Forgot password stranica — email input, success poruka
- [x] Reset password stranica — nova lozinka, prima token iz URL-a
- [x] Must-change-password stranica — za bootstrap korisnike pri prvom loginu
- [x] Auth context/provider — access token u memoriji, refresh token u httpOnly cookie
- [x] Axios interceptor — auto-refresh na 401, silent retry
- [x] Protected route wrapper — redirect na login ako nema tokena
- [x] Role-based route guard — citizen vs officer vs admin

## Citizen layout + routing
- [x] CitizenLayout — header (logo, nav, user dropdown s logout), footer, mobile hamburger
- [x] Routing: /requests/*, /finance/*, /profile
- [x] Početna stranica nakon logina — tri kartice: "Moje financije", "Moji zahtjevi", "Novi zahtjev"
- [x] Lazy loading feature modula

## DynamicFormRenderer (KLJUČNA KOMPONENTA — shared)
- [x] mode="edit" — react-hook-form + zod dinamička schema iz form_schema_snapshot
- [x] mode="readonly" — isti layout ali svi inputi disabled/read-only, za officer i citizen detalj
- [x] mode="preview" — za admin preview tab, submit/upload disabled
- [x] Polje rendereri po field_type: text, textarea, number, date, select, checkbox, oib (s validacijom checksum), iban (s validacijom), email, phone
- [x] Validacija inline na blur: format, min/max, regex, OIB checksum, IBAN checksum
- [x] Validacija na submit: sva obvezna polja popunjena, scroll na prvi error + toast
- [x] i18n-aware labele i help text (hr/en iz field definicije)
- [x] Generous spacing, label iznad, help text ispod, error ispod help texta crvenim

## Admin modul (sekcija 11)
- [ ] AdminLayout — sidebar (Vrste zahtjeva, Korisnici, Audit log), top bar
- [ ] Ekran A — Popis vrsta zahtjeva: tablica desktop / kartice mobile, filtri (aktivni/svi/arhivirani), search, akcije (uredi, deaktiviraj, dupliciraj, obriši)
- [ ] Ekran B Tab 1 — Osnovno: naziv hr/en, interni kod (auto-slug), opis hr/en, status toggle, procijenjeno trajanje, redoslijed
- [ ] Ekran B Tab 2 — Polja forme: lista s drag-drop, modal za dodavanje/uređivanje polja (tip, naziv hr/en, help text, obvezno, napredne postavke collapsible, interni ključ)
- [ ] Ekran B Tab 3 — Privitci: lista s drag-drop, modal (naziv hr/en, opis, obvezan, dozvoljeni formati multi-select, max veličina dropdown, interni ključ)
- [ ] Ekran B Tab 4 — Preview: DynamicFormRenderer mode="preview"
- [ ] Verzioniranje UX: detekcija strukturne promjene u frontendu, modal "Ova vrsta ima X zahtjeva, stvarate v2" s potvrdom
- [ ] Brisanje UX: provjera usage prije brisanja, modal ako ima aktivne zahtjeve
- [ ] Dupliciranje: gumb s potvrdom
- [ ] Svi tekstovi kroz i18n (hr.json ključevi), NULA hardkodiranog teksta

## Građanin — zahtjevi (sekcija 12)
- [ ] Ekran 1 — Odabir vrste zahtjeva: /requests/new, flat lista, search bar, chevron
- [ ] Ekran 2 — Preflight: /requests/new/{code}, naziv, opis, "Što ćete trebati" (obvezna polja + privitci), "Što se događa nakon" (X radnih dana), sticky gumb "Započni zahtjev" (DISABLED ako email nije verificiran + inline upozorenje)
- [ ] Ekran 3 — Forma: /requests/{id}/edit, sticky header s auto-save indikatorom, DynamicFormRenderer mode="edit", sekcija privitci (upload s progress bar, thumbnail/ikona, zamijeni/ukloni), sticky footer (spremi + dalje)
- [ ] Auto-save: debounced 3s PATCH, If-Match ETag, 409 handling (toast "uređeno u drugom tabu"), indikator "Spremljeno u HH:MM"
- [ ] Upload privitka: native file picker, klijentska MIME/veličina validacija, multipart POST, progress bar, retry na fail
- [ ] Ekran 4 — Potvrda: /requests/{id}/review, sažetak svih odgovora s "Uredi" linkovima, privitci, checkbox "Potvrđujem...", sticky footer natrag+pošalji (disabled dok checkbox nije čekiran)
- [ ] Ekran 5 — Potvrda slanja: /requests/{id}/submitted, checkmark, referentni broj (krupno + copy gumb), email info, gumbi za dalje
- [ ] Ekran 6 — Moji zahtjevi: /requests, filter chips (svi/nacrti/u obradi/zaprimljeni/odbijeni), kartice sa status badge, empty state
- [ ] Ekran 7 — Detalj: /requests/{id}, sadržaj po statusu (draft: banner+nastavi/obriši, submitted: read-only+povijest, received: zeleni banner+akt broj, rejected: burgundy banner+razlog+novi zahtjev gumb)
- [ ] Session timeout handling: axios interceptor silent refresh, sessionStorage fallback za formu

## Officer — back-office (sekcija 13)
- [ ] OfficerLayout — sidebar (Inbox s badge brojačem), top bar, desktop-first
- [ ] Ekran A — Inbox: /office/inbox, tabovi (na čekanju/zaprimljeni/odbijeni/svi), tablica desktop / kartice mobile
- [ ] InboxTable — sortable stupci, status indikator (plava točka Novo / siva Pregledano), vrsta, podnositelj, OIB, ref broj, privitci, vrijeme od podnošenja
- [ ] InboxFilters — slide-out panel: vrsta zahtjeva multi-select, datum range, OIB/ref text, sort
- [ ] Search bar — instant po OIB/ime/ref broj/akt ID, debounce 300ms
- [ ] Pagination — 25/50/100, default 25
- [ ] Real-time refresh toast: "1 novi zahtjev (osvježi)" — ne auto-refresh
- [ ] Ekran B — Detalj: /office/requests/{id}, dvostupčani desktop (60/40), jednostupčani mobile
- [ ] Lijevi stupac: vrsta, DynamicFormRenderer mode="readonly", AttachmentList s preview/download
- [ ] Desni stupac: podnositelj info, metapodaci, HistoryTimeline
- [ ] AttachmentPreviewModal — PDF.js za PDF, native <img> za slike, lightbox stil
- [ ] AcceptRequestModal — potvrda tekst, loading state "Zapisivanje u SEUP...", sync success → toast + redirect, async → "U tijeku" + redirect, dead-letter → žuti banner + retry
- [ ] RejectRequestModal — dropdown razlog (iz /office/rejection-reasons), textarea internal note (obvezan ako razlog=other), upozorenje "Podnositelj će biti obaviješten"
- [ ] OutboxStatusIndicator — "U procesiranju" badge za zahtjeve čiji outbox je aktivan
- [ ] Sidebar badge: unread-count, polling svakih 30s

## i18n
- [ ] hr.json — svi ključevi za sve gore navedene ekrane
- [ ] en.json — prazni placeholder-i (isti ključevi, prazne vrijednosti ili engleski ako usput)
- [ ] Provjera: NULA hardkodiranog hrvatskog teksta u komponentama
