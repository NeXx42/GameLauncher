OUTPUT_DIR = ./_Output/AvaloniaUI

publish:

	rm -rf ${OUTPUT_DIR}

	dotnet publish GameLibrary.Avalonia/GameLibrary.Avalonia.csproj \
		-c Release \
		-r linux-x64 \
		--self-contained true \
		/p:PublishSingleFile=true \
		/p:IncludeAllContentForSelfExtract=true \
		-o ${OUTPUT_DIR}