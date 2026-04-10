using Portal.Application.Interfaces;

namespace Portal.Infrastructure.LocalDb;

public class LocalDbAktWriter : ILocalDbAktWriter
{
    public Task<AktWriteResult> WriteAktAsync(WriteAktCommand cmd, CancellationToken ct)
    {
        // TODO: Implement when SEUP table schema is available (CLAUDE.md section 21.1)
        throw new NotImplementedException(
            "LocalDbAktWriter is not yet implemented. Awaiting SEUP table schema (tblAkti, tblPRBiljeska, tblDatoteke).");
    }
}
