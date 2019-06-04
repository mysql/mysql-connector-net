#!/bin/sh

#INFO_BIN begin ---
INFO_BIN=INFO_BIN
BUILD_DATE=$(date '+%Y-%m-%d %H:%M:%S')
OS=$(uname)
PLATFORM=""
VERSION=""
BUILD_TYPE=$1

#get OS name from 'uname' (CMAKE_SYSTEM) variable
case "$OS" in
  Darwin*)  PLATFORM="OSX" ;;
  Linux*)   PLATFORM="LINUX" ;;
  MINGW*)   PLATFORM="WINDOWS" ;;
esac

#get OS version from 'uname' (CMAKE_SYSTEM) variable
if [ $PLATFORM = "WINDOWS" ]
then
    VERSION=$(echo ${OS}| cut -d'-' -f 2)
else
    VERSION=$(uname -r| cut -d'-' -f 1)
fi
#write output to INFO_BIN file
(
    echo build-date: ${BUILD_DATE} $'\r' 
    echo os-info: ${PLATFORM}'-'${VERSION} $'\r' 
    echo build-type: ${BUILD_TYPE}
) > ${INFO_BIN}

mv INFO_BIN ../../../../..
#INFO_BIN end ---

#INFO_SRC begin ---
INFO_SRC=INFO_SRC
SRC_VERSION=$2
GIT_FLAG=1

git branch || GIT_FLAG=2

if [ $GIT_FLAG -eq 1 ]
then
#write output to INFO_SRC file
  (
      echo version: ${SRC_VERSION} $'\r'
      echo 'branch: '; git branch | sed -n -e 's/^\* \(.*\)/\1/p'; echo $'\r'
      git log -n 1 --date=format:'%c' --pretty=format:'date: %ad'; echo $'\r'
      git log -n 1 --date=format:'%ci' --pretty=format:'commit: %H'; echo $'\r'
      git log -n 1 --date=format:'%ci' --pretty=format:'short: %h'
  ) > ${INFO_SRC}
else
 #write output to INFO_SRC file
 (
     echo version: ${SRC_VERSION}
 ) > ${INFO_SRC}
fi

mv INFO_SRC ../../../../..
#INFO_SRC end ---