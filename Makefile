OUTPUT_DIR = ./_Output/

publish:

	rm -rf ${OUTPUT_DIR}
	
	# app
	dotnet publish GameLibrary.Avalonia/GameLibrary.Avalonia.csproj \
		-c Release \
		-r linux-x64 \
		--self-contained true \
		/p:PublishSingleFile=false \
		/p:IncludeAllContentForSelfExtract=true \
		-o ${OUTPUT_DIR}/Avalonia
		
	# gtk overlay
	cd GameLibrary_GTKOverlay && cargo build --release
	cp GameLibrary_GTKOverlay/target/release/GameLibrary_GTKOverlay ${OUTPUT_DIR}/Avalonia/
		
	tar -czvf ${OUTPUT_DIR}/GameLibrary.Avalonia.tar.gz -C ${OUTPUT_DIR} Avalonia