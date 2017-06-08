#!/usr/bin/python

import xml.etree.ElementTree as ET
import os
import sys
import fnmatch
import re

defaultFiles = "*.csproj"
defaultDir = "../"
version = ""

def LookFor(dir, file):
  for root, dirnames, filenames in os.walk(dir):
    for filename in fnmatch.filter(filenames, file):
      fullname = os.path.join(root, filename)
      updateVersion(os.path.join(root, filename))


def updateVersion(file):
  global version
  root = ET.parse(file)
  e = root.findall("./PropertyGroup/Version")
  if e:
    e[0].text = version
    root.write(file)
    print "File updated", file



if len(sys.argv) < 2:
  print "setVersion.py <version>"
  sys.exit(1)
else:
  version = sys.argv[1]
  if not re.match("\d{1,2}\.\d{1,2}\.\d{1,2}(-\w+)?", version):
    print "Version is invalid", version
    sys.exit(1)
  print "Version to update:", version

if len(sys.argv) > 2:
  for arg in sys.argv[2:]:
    split = os.path.split(arg)
    dir = split[0]
    file = split[1]
    if dir == "":
      dir = defaultDir
    if file == "":
      file = defaultFiles
    print "dir:", dir, "files:", file
    LookFor(dir, file)
else:
  LookFor(defaultDir, defaultFiles)
sys.exit(0)
