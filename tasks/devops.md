# DevOps zadaci — Sprint 1

## Docker (scaffold pokriva osnovno, ovo su dorade)
- [ ] Provjeri da docker-compose up pokreće api + web + postgres + mailhog
- [ ] Provjeri da EF Core migracije prolaze automatski na startupu
- [ ] Provjeri health check: curl localhost:5000/health/live → 200
- [ ] Provjeri da frontend dev server radi s hot reload
- [ ] .env.example — dokumentiraj sve varijable

## GitHub Actions CI
- [ ] Workflow: on push to main i na PR
- [ ] Step 1: dotnet restore + build
- [ ] Step 2: dotnet test (unit + architecture tests)
- [ ] Step 3: npm ci + npm run build (frontend)
- [ ] Step 4: npm run lint (frontend)
- [ ] Destructive migration check: regex scan generiranog SQL-a za DROP/DELETE/rename → auto-label migration:destructive
- [ ] Block merge ako architecture tests fail

## Backup infrastruktura (sekcija 19.1)
- [ ] pg_dump skripta za portal bazu
- [ ] deployment_backups tablica (migracija)
- [ ] Cron job: pg_dump + verify restore + INSERT INTO deployment_backups
- [ ] MigrationRunner backup check — odbija krenuti ako nema backupa < 60 min (za produkciju, ne dev)

## Monitoring osnova
- [ ] Serilog → structured JSON output
- [ ] Docker healthcheck u Dockerfile-ovima
- [ ] Provjeri da logovi ne sadrže OIB u čistom obliku (masking test)
