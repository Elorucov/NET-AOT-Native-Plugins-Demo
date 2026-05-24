# Makefile for building plugin
#
# Linux/macOS:  make
# Windows:      using CMakeLists.txt or cl.exe manually

CC      ?= gcc
CFLAGS  := -O2 -Wall -fPIC
TARGET  := libplugin.so

ifeq ($(shell uname -s), Darwin)
    TARGET  := libplugin.dylib
    LDFLAGS := -dynamiclib
else
    LDFLAGS := -shared
endif

.PHONY: all clean

all: $(TARGET)

$(TARGET): plugin.c
	$(CC) $(CFLAGS) $(LDFLAGS) -o $@ $^
	@echo "Built: $(TARGET)"

clean:
	rm -f $(TARGET)
