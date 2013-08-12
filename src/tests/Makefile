SUBDIRS=DekiMisc CheckDb

all:
	@failcom='exit 1'; \
	if test -z "$(root_dir)"; then \
	  echo "You must set root_dir on the build line: Try \"make root_dir=\`pwd\` PREFIX=\`pwd\`/bin\""; \
	  exit 1; \
	fi; \
	if test -f "$(addprefix $(PREFIX)/, log4net.dll)"; then \
	  echo "log4net present in prefix."; \
	else \
	  cp $(addprefix $(root_dir)/, redist/log4net.dll) $(addprefix $(PREFIX)/, log4net.dll); \
	  gacutil /i redist/log4net.dll; \
	fi; \
	target=`echo $@ | sed s/-recursive//`; \
	list='$(SUBDIRS)'; for subdir in $$list; do \
	  echo "Making $$target in $$subdir"; \
	  if test "$$subdir" = "."; then \
	    dot_seen=yes; \
	  else \
	    local_target="$$target"; \
	    (cd $$subdir && $(MAKE) $(AM_MAKEFLAGS) $$local_target) \
	    || eval $$failcom; \
	  fi; \
	done; \
	test -z "$$fail"

clean:
	@failcom='exit 1'; \
	if test -z "$(root_dir)"; then \
	  echo "You must set root_dir on the build line: Try \"make root_dir=\`pwd\` PREFIX=\`pwd\`/bin\""; \
	  exit 1; \
	fi; \
	list='$(SUBDIRS)' ; \
	rev=''; for subdir in $$list; do \
	  if test "$$subdir" = "."; then :; else \
	    rev="$$subdir $$rev"; \
	  fi; \
	done; \
	rev="$$rev ."; \
	target=`echo $@ | sed s/-recursive//`; \
	for subdir in $$rev; do \
	  echo "Making $$target in $$subdir"; \
	  if test "$$subdir" = "."; then :; else \
	    local_target="$$target"; \
	    (cd $$subdir && $(MAKE) $(AM_MAKEFLAGS) $$local_target) \
	    || eval $$failcom; \
	  fi; \
	done && test -z "$$fail"


all-am: Makefile
clean-am: 
