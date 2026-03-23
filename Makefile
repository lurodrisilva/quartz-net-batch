.PHONY: restore build test publish docker clean

SOLUTION   := BatchScheduler.sln
DOCKERFILE := deploy/docker/Dockerfile
IMAGE_NAME := batch-scheduler
IMAGE_TAG  := local

restore:
	dotnet restore $(SOLUTION)

build: restore
	dotnet build $(SOLUTION) -c Release --no-restore

test: build
	dotnet test $(SOLUTION) -c Release --no-build

publish: restore
	dotnet publish src/Batch.Worker/Batch.Worker.csproj -c Release -o artifacts/publish

docker:
	docker build -f $(DOCKERFILE) -t $(IMAGE_NAME):$(IMAGE_TAG) .

clean:
	dotnet clean $(SOLUTION) -c Release
	rm -rf artifacts
