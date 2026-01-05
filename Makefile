OUTPUT_DIR = ./_Output/

publish:

	rm -rf ${OUTPUT_DIR}/*
	
	# app
	dotnet publish GameLibrary.AvaloniaUI/GameLibrary.AvaloniaUI.csproj \
		-c Release \
		-r linux-x64 \
		--self-contained true \
		/p:PublishSingleFile=false \
		/p:IncludeAllContentForSelfExtract=true \
		-o ${OUTPUT_DIR}/AvaloniaUI
		
	# gtk overlay
	cd GameLibrary_GTKOverlay && cargo build --release
	cp GameLibrary_GTKOverlay/target/release/GameLibrary_GTKOverlay ${OUTPUT_DIR}/AvaloniaUI/
		
	tar -czvf ${OUTPUT_DIR}/GameLibrary.AvaloniaUI.tar.gz -C ${OUTPUT_DIR} AvaloniaUI