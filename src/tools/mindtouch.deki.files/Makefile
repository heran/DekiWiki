include $(root_dir)/Makefile.include

ASSEMBLY_NAME=mindtouch.deki.files
ASSEMBLY=$(ASSEMBLY_NAME).exe

TARGET=$(addprefix $(build_prefix)/, $(ASSEMBLY))

sources = 							\
		Program.cs					\
		Properties/AssemblyInfo.cs

references = 																\
		-r:System.Data														\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.data.dll)					\
		-r:$(addprefix $(build_prefix)/, mindtouch.deki.dll)					\
		-r:$(addprefix $(root_dir)/, redist/MySql.Data.dll)					\
		-r:$(addprefix $(root_dir)/, redist/mindtouch.dream.dll)				\

$(TARGET): $(sources)
	$(MCS) /out:$(TARGET) /target:exe $(references) $(sources)

all: $(TARGET) 
