include $(root_dir)/Makefile.include
ASSEMBLY_NAME=mindtouch.deki.tests
ASSEMBLY=$(ASSEMBLY_NAME).dll
TARGET=$(addprefix $(build_prefix)/, $(ASSEMBLY))

sources = \
                FileTests/IndexTests.cs \
                FileTests/MoveTests.cs  \
                FileTests/GetTests.cs   \
                FileTests/DescriptionTests.cs   \
                FileTests/DeleteTests.cs        \
                FileTests/UploadTests.cs        \
                FileTests/RevisionTests.cs		\
                FileUtils.cs    \
                AssemblyInfo.cs \
                ChangeSubscriptionTests/DekiChangeSubscriptionExtTests.cs	\
                ChangeSubscriptionTests/DekiChangeSubscriptionTests.cs		\
                ChangeSubscriptionTests/NotificationDelayQueueTests.cs		\
                ChangeSubscriptionTests/PageChangeCacheTests.cs				\
                DateTimeParsingTests.cs	\
                DekiExceptionMapperTests.cs \
                DekiInstanceTests.cs	\
                DekiResourceBuilderTests.cs \
                DekiResourcesTests.cs   \
                DekiPubSubTests.cs		\
                EntityTests/ResourceContentBETests.cs   \
                ExternalLuceneWireupTests.cs	\
                LicenseUtil.cs                  \
                SearchTests/SearchBLTests.cs    \
                SearchTests/SearchSerializerTests.cs    \
                SearchTests/SearchResultRankCalculatorTests.cs  \
                SiteTests/SearchTests.cs        \
                SiteTests/SearchTestsWithSearchBLMock.cs    \
                SiteTests/BanTests.cs   \
                SiteTests/ServicesTests.cs      \
                SiteTests/OtherTests.cs \
                SiteTests/RolesTests.cs \
                SiteTests/SettingsTests.cs      \
                SiteTests/NavTests.cs   \
                Utils.cs        \
                UserTests/UsersTest.cs  \
                UserUtils.cs    \
                PageTests/ContentTests.cs       \
                PageTests/DekiScriptTests.cs       \
                PageTests/RevertTests.cs        \
                PageTests/RevisionTests.cs      \
                PageTests/MoveTests.cs  \
                PageTests/OtherTests.cs \
                PageTests/DeleteTests.cs        \
                PageTests/SecurityTests.cs      \
                PageTests/CommentTests.cs       \
                PageTests/TemplateTests.cs      \
                ServiceTests/XmlAuthTests.cs    \
                ServiceTests/WebCacheTests.cs    \
                PageUtils.cs    \
                TitleTests.cs	\
                PlainTextResourceManagerTests.cs	\
                ArchiveTests/RestoreFile.cs     \
                ArchiveTests/RestorePage.cs     \
                ArchiveTests/FilesTests.cs      \
                ArchiveTests/PagesTests.cs      \
                ArchiveTests/GetArchiveTests.cs \
                GroupTests/GroupsTest.cs        \
                PropertyTests/PropertyTests.cs	\
                TagTests/TagTests.cs	\
                VarnishTests/UpdateDelayQueueTests.cs	\
                VarnishTests/VarnishPurgeServiceTests.cs \
                RatingTests/RatingTests.cs

references = \
                -r:System \
                -r:System.Data \
                -r:System.Xml \
                -r:System.Web \
                -r:System.Drawing \
                -r:System.Configuration \
                -r:$(addprefix $(build_prefix)/, mindtouch.deki.data.dll)		\
                -r:$(addprefix $(build_prefix)/, mindtouch.deki.dll)			\
                -r:$(addprefix $(build_prefix)/, mindtouch.deki.util.dll)		\
                -r:$(addprefix $(build_prefix)/, mindtouch.deki.script.dll)		\
                -r:$(addprefix $(root_dir)/, redist/mindtouch.core.dll)         \
                -r:$(addprefix $(root_dir)/, redist/mindtouch.dream.dll)        \
                -r:$(addprefix $(root_dir)/, redist/mindtouch.dream.test.dll)   \
                -r:$(addprefix $(root_dir)/, redist/nunit/nunit.framework.dll)  \
        		-r:$(addprefix $(root_dir)/, redist/Autofac.dll)					\
                -r:$(addprefix $(root_dir)/, redist/log4net.dll)    \
                -r:$(addprefix $(root_dir)/, redist/Moq.dll)    \
                -r:$(addprefix $(root_dir)/, redist/SgmlReaderDll.dll)  \
                -r:$(addprefix $(root_dir)/, redist/Lucene.Net.dll) 

$(TARGET): $(sources)
	$(MCS) $(MCS_FLAGS) /out:$(TARGET) /target:library $(references) $(sources) 

all: $(TARGET)
