using GameLibrary.DB.Tables;
using GameLibrary.Logic.Enums;

namespace GameLibrary.Logic.Objects;


public class LibraryDto
{
    public readonly int libraryId;
    public string root { protected set; get; }
    public Library_ExternalProviders? externalType { protected set; get; }

    public LibraryDto(dbo_Libraries lib)
    {
        libraryId = lib.libaryId;
        root = lib.rootPath;
        externalType = lib.libraryExternalType.HasValue ? (Library_ExternalProviders)lib.libraryExternalType : null;
    }
}
