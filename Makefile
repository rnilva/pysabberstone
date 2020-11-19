root_dir:=$(shell pwd)
build_dir:=_sabberstone_dotnet

run-mmf:
	dotnet $(build_dir)/SabberStonePython.dll mmf 0

run-rpc:
	dotnet $(build_dir)/SabberStonePython.dll rpc 50052

build:
	@echo $(root_dir)
	dotnet build -c Release -o "$(root_dir)/$(build_dir)" dotnet_core
	
clean:
	dotnet clean -o $(root_dir) dotnet_core
