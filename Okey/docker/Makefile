REGISTRY=registry.app.unistra.fr/okaybytes/okey

IMG_NAME_CICD=projet-integrateur-cicd
IMG_NAME_API=okeyapi
IMG_NAME_SERVER=okeyserver
IMG_NAME_DB-INIT=db-init
IMG_NAME_WEBSITE=okeywebsite

.PHONY: all clean start-extra build-extra stop-extra start-core build-core stop-core

all: cicd api server db-init website

cicd: $(IMG_NAME_CICD)
$(IMG_NAME_CICD): CICD.Dockerfile ../requirements.txt ../.config/dotnet-tools.json
	docker build -t $(REGISTRY)/$@ -f $< ..
	touch $@

api: $(IMG_NAME_API)
$(IMG_NAME_API): OkeyApi.Dockerfile OkeyApi.Dockerfile.dockerignore ../server/OkeyApi/
	docker build -t $(REGISTRY)/$@ -f $< ..
	touch $@

server: $(IMG_NAME_SERVER)
$(IMG_NAME_SERVER): OkeyServer.Dockerfile OkeyServer.Dockerfile.dockerignore ../server/OkeyServer/
	docker build -t $(REGISTRY)/$@ -f $< ..
	touch $@

db-init: $(IMG_NAME_DB-INIT)
$(IMG_NAME_DB-INIT): DB-Init.Dockerfile DB-Init.Dockerfile.dockerignore ../server/OkeyApi/
	docker build -t $(REGISTRY)/$@ -f $< ..
	touch $@

website: $(IMG_NAME_WEBSITE)
$(IMG_NAME_WEBSITE): OkeyWebsite.Dockerfile OkeyWebsite.Dockerfile.dockerignore ../server/okeywebsite/new_app/
	docker build -t $(REGISTRY)/$@ -f $< ..
	touch $@

start-extra:
	docker compose --profile extra up -d

build-extra:
	@echo "Makefile: don't forget to first remove Okey related containers/images."
	docker compose --profile extra build

stop-extra:
	docker compose --profile extra down

start-core:
	docker compose --profile core up -d

build-core:
	@echo "Makefile: don't forget to first remove Okey related containers/images."
	docker compose --profile core build

stop-core:
	docker compose --profile core down

clean:
	rm -rf $(IMG_NAME_CICD) $(IMG_NAME_API) $(IMG_NAME_SERVER) $(IMG_NAME_DB-INIT) $(IMG_NAME_WEBSITE) docker_data/data/
	@echo "Makefile: artifacts cleaned."
	@echo "Makefile: this does not remove the docker images or containers. Use 'docker rmi' and/or 'docker container rm' for that."
	@echo "Makefile: compose persistant data cleaned."
