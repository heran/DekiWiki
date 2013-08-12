include $(root_dir)/Makefile.include
ASSEMBLY_NAME=mindtouch.deki.checkdb
ASSEMBLY=$(ASSEMBLY_NAME).exe
TARGET=$(addprefix $(build_prefix)/, $(ASSEMBLY))

sources = \
                DatabaseTests.cs \
                AssemblyInfo.cs \

references = \
                -r:System \
                -r:System.Data \
                -r:System.Xml \
                -r:System.Configuration \
                -r:$(addprefix $(root_dir)/, redist/mindtouch.dream.dll) \
                -r:$(addprefix $(root_dir)/, redist/MySql.Data.dll) \
                -r:$(addprefix $(root_dir)/, redist/nunit/nunit.framework.dll)

$(TARGET): 
	$(MCS) $(MCS_FLAGS) /out:$(TARGET) /target:exe $(references) $(sources) 

$(addprefix $(build_prefix)/, mindtouch.deki.checkdb.schema.xml): mindtouch.deki.checkdb.schema.xml
	cp -v mindtouch.deki.checkdb.schema.xml $(addprefix $(build_prefix)/, mindtouch.deki.checkdb.schema.xml)

$(addprefix $(build_prefix)/, mindtouch.deki.checkdb.config.xml): mindtouch.deki.checkdb.config.xml
	cp -v mindtouch.deki.checkdb.config.xml $(addprefix $(build_prefix)/, mindtouch.deki.checkdb.config.xml)

all: $(TARGET) $(addprefix $(build_prefix)/, mindtouch.deki.checkdb.schema.xml) $(addprefix $(build_prefix)/, mindtouch.deki.checkdb.config.xml)
