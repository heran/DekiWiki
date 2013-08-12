include $(root_dir)/Makefile.include

ASSEMBLY_NAME=mindtouch.deki.util
ASSEMBLY=$(ASSEMBLY_NAME).dll

TARGET=$(addprefix $(build_prefix)/, $(ASSEMBLY))

sources = 							\
		DekiLicense.cs				\
		Export/ArchivePackageWriter.cs	\
		Export/Exporter.cs			\
		Export/ExportException.cs	\
		Export/ExportItem.cs		\
		Export/ExportManager.cs		\
		Export/FilePackageWriter.cs	\
		Export/IPackageWriter.cs	\
		Export/PackageWriterBase.cs	\
		Import/ArchivePackageReader.cs	\
		Import/BulkCopy.cs			\
		Import/FilePackageReader.cs	\
		Import/Importer.cs			\
		Import/ImportException.cs	\
		Import/ImportItem.cs			\
		Import/ImportManager.cs		\
		Import/IPackageReader.cs	\
		Properties/AssemblyInfo.cs  \
		Search/LuceneClauseBuilder.cs   \
		Search/LuceneClauseParser.cs   \
		Search/QueryTerm.cs         \
		Search/QueryTermParser.cs   \
		Search/SearchQuery.cs \
		Search/SearchQueryParser.cs \
		Search/SearchQueryParserType.cs \
		Title.cs                    \
		Types.cs

references = 																\
		-r:System															\
		-r:System.Data														\
		-r:System.Web														\
		-r:System.Xml														\
		-r:$(addprefix $(build_prefix)/, mindtouch.dream.dll)				\
		-r:$(addprefix $(root_dir)/, redist/log4net.dll)					\
		-r:$(addprefix $(root_dir)/, redist/ICSharpCode.SharpZipLib.dll)					\
		-r:$(addprefix $(root_dir)/, redist/SgmlReaderDll.dll)				

$(addprefix $(build_prefix)/, Autofac.dll): $(addprefix $(root_dir)/, redist/Autofac.dll)
	cp $(addprefix $(root_dir)/, redist/Autofac.dll) $(addprefix $(build_prefix)/, Autofac.dll)
	
$(addprefix $(build_prefix)/, mindtouch.dream.dll): $(addprefix $(root_dir)/, redist/mindtouch.dream.dll)
	cp $(addprefix $(root_dir)/, redist/mindtouch.dream.dll) $(addprefix $(build_prefix)/, mindtouch.dream.dll)

$(addprefix $(build_prefix)/, SgmlReaderDll.dll): $(addprefix $(root_dir)/, redist/SgmlReaderDll.dll)
	cp $(addprefix $(root_dir)/, redist/SgmlReaderDll.dll) $(addprefix $(build_prefix)/, SgmlReaderDll.dll)

$(addprefix $(build_prefix)/, ICSharpCode.SharpZipLib.dll): $(addprefix $(root_dir)/, redist/ICSharpCode.SharpZipLib.dll)
	cp $(addprefix $(root_dir)/, redist/ICSharpCode.SharpZipLib.dll) $(addprefix $(build_prefix)/, ICSharpCode.SharpZipLib.dll)

$(TARGET): $(sources) $(addprefix $(build_prefix)/, mindtouch.dream.dll) $(addprefix $(build_prefix)/, SgmlReaderDll.dll) $(addprefix $(build_prefix)/, ICSharpCode.SharpZipLib.dll) $(addprefix $(build_prefix)/, Autofac.dll) 
	$(MCS) $(MCS_FLAGS) /out:$(TARGET) /target:library $(references) $(sources)

all: $(TARGET)
