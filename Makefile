# Makefile for KIS
#
# If you have KSP in a non-standard directory, be sure to set the environment variable
# $XDG_DATA_HOME (if different from usual), or $KSPDIR to the directory of KSP (if using
# a Steam library in a non-standard location).
#
# Example: if you have a Steam library in the directory /media/partition3, do something like:
# export KSPDIR="/media/partition3/SteamLibrary/SteamApps/common/Kerbal Space Program"
# then, run make as normal.

ifeq ($(OS),Windows_NT)
	# It's unlikely that a windows user will use a makefile to compile KIS
	# Visual studio is free, after all.
else
	UNAME_S := $(shell uname -s)
	ifeq ($(UNAME_S),Linux)
		ifndef XDG_DATA_HOME
			XDG_DATA_HOME := ${HOME}/.local/share
		endif
		ifndef KSPDIR
			KSPDIR := ${XDG_DATA_HOME}/Steam/SteamApps/common/Kerbal\ Space\ Program
		endif
		MANAGED := ${KSPDIR}/KSP_Data/Managed/
	endif
	ifeq ($(UNAME_S),Darwin)
		ifndef KSPDIR
			KSPDIR := ${HOME}/Library/Application\ Support/Steam/SteamApps/common/Kerbal\ Space\ Program/
		endif
		MANAGED := ${KSPDIR}/KSP.app/Contents/Data/Managed/
	endif
endif

KISFILES := $(wildcard Source/*.cs) \
	$(wildcard Source/Properties/*.cs)

RESGEN2 := resgen2
GMCS := mcs
GIT := git
TAR := tar
ZIP := zip

VERSION := $(shell ${GIT} describe --tags --always)

all: build

info:
	@echo "== KIS Build Information =="
	@echo " resgen2: ${RESGEN2}"
	@echo " gmcs: ${GMCS}"
	@echo " git: ${GIT}"
	@echo " tar: ${TAR}"
	@echo " zip: ${ZIP}"
	@echo " KSP Data: ${KSPDIR}"
	@echo " KIS Files: ${KISFILES}"
	@echo "================================"

build: build/KIS.dll

build/%.dll: ${KISFILES}
	mkdir -p build
	${GMCS} -t:library -lib:"${MANAGED}" \
		-r:Assembly-CSharp,Assembly-CSharp-firstpass \
		-r:UnityEngine,UnityEngine.UI \
		-lib:Binaries -r:KSPDev_Utils.0.30 \
		-out:$@ \
		${KISFILES}

package: build ${KISFILES}
	mkdir -p package/KIS/Plugins
	cp -r Parts package/KIS/
	cp -r Sounds package/KIS/
	cp -r Textures package/KIS/
	cp build/KIS.dll package/KIS/Plugins/
	cp LICENSE.md README.md package/KIS/

%.tar.gz:
	${TAR} zcf $@ package/KIS

tar.gz: package KIS-${VERSION}.tar.gz

%.zip:
	${ZIP} -9 -r $@ package/KIS

zip: package KIS-${VERSION}.zip

clean:
	@echo "Cleaning up build and package directories..."
	rm -rf build/ package/

install: build
	mkdir -p ${KSPDIR}/GameData/KIS/Plugins
	cp -r Parts ${KSPDIR}/GameData/KIS/
	cp -r Sounds ${KSPDIR}/GameData/KIS/
	cp -r Textures ${KSPDIR}/GameData/KIS/
	cp build/KIS.dll ${KSPDIR}/GameData/KIS/Plugins/

uninstall: info
	rm -rf ${KSPDIR}/GameData/KIS/Plugins
	rm -rf ${KSPDIR}/GameData/KIS/Parts

.PHONY : all info build package tar.gz zip clean install uninstall)
