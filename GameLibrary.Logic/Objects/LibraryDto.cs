using GameLibrary.DB.Tables;

namespace GameLibrary.Logic.Objects;


public class LibraryDto
{
    public enum ExternalTypes
    {
        Steam = 1
    }

    public readonly int libraryId;
    public string root { protected set; get; }
    public ExternalTypes? externalType { protected set; get; }

    public LibraryDto(dbo_Libraries lib)
    {
        libraryId = lib.libaryId;
        root = lib.rootPath;
        externalType = (ExternalTypes)lib.libraryExternalType;
    }
}
