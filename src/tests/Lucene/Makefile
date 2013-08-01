include $(root_dir)/Makefile.include
ASSEMBLY_NAME=mindtouch.lucence.tests
ASSEMBLY=$(ASSEMBLY_NAME).dll
TARGET=$(addprefix $(build_prefix)/, $(ASSEMBLY))

sources = \
                AssemblyInfo.cs \
                DocumentTests.cs    \
                Html2TextTests.cs   \
                QueryTests.cs   \
                TestIndex.cs    \
                TokenizerTests.cs   \
                UpdateDelayQueueTests.cs

references = \
                -r:System \
                -r:System.Data \
                -r:System.Xml \
                -r:System.Drawing \
                -r:System.Configuration \
                -r:$(addprefix $(root_dir)/, redist/mindtouch.dream.dll) \
                -r:$(addprefix $(root_dir)/, ../dist/mindtouch.indexservice.dll)    \
                -r:$(addprefix $(root_dir)/, redist/nunit/nunit.framework.dll) \
                -r:$(addprefix $(root_dir)/, redist/log4net.dll)    \
                -r:$(addprefix $(root_dir)/, redist/SgmlReaderDll.dll)  \
                -r:$(addprefix $(root_dir)/, redist/Lucene.Net.dll) 

$(TARGET): 
	$(MCS) $(MCS_FLAGS) /out:$(TARGET) /target:library $(references) $(sources) 

all: $(TARGET)
