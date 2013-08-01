include $(root_dir)/Makefile.include

ASSEMBLY_NAME=mindtouch.deki.script
ASSEMBLY=$(ASSEMBLY_NAME).dll

TARGET=$(addprefix $(build_prefix)/, $(ASSEMBLY))

sources = 							\
		AssemblyInfo.cs				\
		CsvParser.cs				\
		DekiScript.cs				\
		DekiScriptAttribute.cs		\
		DekiScriptEvalContext.cs	\
		DekiScriptInterpreter.cs	\
		DekiScriptRuntime.cs		\
		SimpleHtmlFormatter.cs		\
		XDekiScript.cs				\
		XmlNodePlainTextReadonlyByteStream.cs		\
		Compiler/DekiJemProcessor.cs				\
		Compiler/DekiScriptExpressionEvaluation.cs	\
		Compiler/DekiScriptExpressionEvaluationState.cs	\
		Compiler/DekiScriptExpressionOptimizer.cs	\
		Compiler/DekiScriptGeneratorEvaluation.cs	\
		Compiler/DekiScriptParser.cs				\
		Compiler/DekiScriptParserException.cs		\
		Compiler/DekiScriptXmlParser.cs				\
		Compiler/Parser.cs				\
		Compiler/Scanner.cs				\
		Expr/DekiScriptAbort.cs			\
		Expr/DekiScriptAccess.cs		\
		Expr/DekiScriptAssign.cs		\
		Expr/DekiScriptBinary.cs		\
		Expr/DekiScriptBool.cs			\
		Expr/DekiScriptCall.cs			\
		Expr/DekiScriptComplexLiteral.cs	\
		Expr/DekiScriptDiscard.cs			\
		Expr/DekiScriptExpression.cs		\
		Expr/DekiScriptExpressionTextWriter.cs \
		Expr/DekiScriptForeach.cs			\
		Expr/DekiScriptGenerator.cs			\
		Expr/DekiScriptGeneratorForeachKeyValue.cs	\
		Expr/DekiScriptGeneratorForeachValues.cs	\
		Expr/DekiScriptGeneratorIf.cs				\
		Expr/DekiScriptGeneratorVar.cs				\
		Expr/DekiScriptList.cs				\
		Expr/DekiScriptListConstructor.cs	\
		Expr/DekiScriptLiteral.cs			\
		Expr/DekiScriptMagicId.cs			\
		Expr/DekiScriptMap.cs				\
		Expr/DekiScriptMapConstructor.cs	\
		Expr/DekiScriptNil.cs				\
		Expr/DekiScriptNumber.cs			\
		Expr/DekiScriptOperation.cs			\
		Expr/DekiScriptReturn.cs			\
		Expr/DekiScriptReturnScope.cs   \
		Expr/DekiScriptSequence.cs			\
		Expr/DekiScriptString.cs			\
		Expr/DekiScriptSwitch.cs			\
		Expr/DekiScriptTernary.cs			\
		Expr/DekiScriptTryCatchFinally.cs	\
		Expr/DekiScriptUnary.cs				\
		Expr/DekiScriptUnknown.cs			\
		Expr/DekiScriptUri.cs				\
		Expr/DekiScriptVar.cs				\
		Expr/DekiScriptXml.cs				\
		Expr/DekiScriptXmlElement.cs				\
		Expr/IDekiScriptExpressionVisitor			\
		Expr/IDekiScriptGeneratorVisitor.cs			\
		Runtime/Library/DekiScriptLibrary-Culture.cs \
		Runtime/Library/DekiScriptLibrary-Date.cs	\
		Runtime/Library/DekiScriptLibrary-Json.cs	\
		Runtime/Library/DekiScriptLibrary-List.cs	\
		Runtime/Library/DekiScriptLibrary-Map.cs	\
		Runtime/Library/DekiScriptLibrary-Number.cs	\
		Runtime/Library/DekiScriptLibrary-String.cs	\
		Runtime/Library/DekiScriptLibrary-Uri.cs	\
		Runtime/Library/DekiScriptLibrary-Web.cs	\
		Runtime/Library/DekiScriptLibrary-Xml.cs	\
		Runtime/Library/DekiScriptLibrary.cs		\
		Runtime/TargetInvocation/ADekiScriptInvocationTarget.cs				\
		Runtime/TargetInvocation/DekiScriptExpressionInvocationTarget.cs	\
		Runtime/TargetInvocation/DekiScriptHttpGetInvocationTarget.cs		\
		Runtime/TargetInvocation/DekiScriptInvocationTargetDescriptor.cs	\
		Runtime/TargetInvocation/DekiScriptNativeInvocationTarget.cs		\
		Runtime/TargetInvocation/DekiScriptRemoteInvocationTarget.cs		\
		Runtime/TargetInvocation/DekiScriptXmlRpcInvocationTarget.cs		\
		Runtime/TargetInvocation/IDekiScriptInvocationTarget.cs				\
		Runtime/DekiScriptControlFlowException.cs	\
		Runtime/DekiScriptEnv.cs					\
		Runtime/DekiScriptException.cs				\
		Runtime/DekiScriptFatalException.cs			\
		Runtime/DekiScriptOutputBuffer.cs			\
		Runtime/DekiScriptOutputProcessor.cs		\
		Runtime/DekiScriptParameter.cs



references = 																\
		-r:System															\
		-r:System.Xml														\
		-r:System.Xml.Linq													\
		-r:$(addprefix $(root_dir)/, redist/mindtouch.dream.dll)			\
		-r:$(addprefix $(root_dir)/, redist/log4net.dll)					\
		-r:$(addprefix $(root_dir)/, redist/SgmlReaderDll.dll)				\
		-resource:'Resources/xhtml-safe.txt,MindTouch.Deki.Script.Resources.xhtml-safe.txt'		\
		-resource:'Resources/xhtml-unsafe.txt,MindTouch.Deki.Script.Resources.xhtml-unsafe.txt'	\
		-resource:'Resources/HtmlEntities.dtd,MindTouch.Deki.Script.Resources.HtmlEntities.dtd'

$(TARGET): $(sources) $(addprefix $(build_prefix)/, mindtouch.dream.dll) $(addprefix $(build_prefix)/, SgmlReaderDll.dll) $(addprefix $(build_prefix)/, log4net.dll)
	$(MCS) $(MCS_FLAGS) /out:$(TARGET) /target:library $(references) $(sources)

all: $(TARGET) 
