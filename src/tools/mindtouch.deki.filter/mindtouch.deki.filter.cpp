/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
 * please review the licensing section.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 * http://www.gnu.org/copyleft/gpl.html
 */

/*
 * Program: mindtouch.deki.filter.exe
 *
 * Purpose: Convert from a file to text.
 *
 * Usage:   mindtouch.deki.filter.exe <extension> <temppath> < input > output
 *
 * Example: mindtouch.deki.filter.exe "doc" "c:\windows\temp" < "c:\temp\sample.doc" > output.txt
 *
 * Notes:   This application expects the file to be sent as a binary stream through stdin.  
 *          The first parameter specifies the file extension.  The second parameter specifies 
 *          the temporary directory.  If successful, the application exits with status 0 
 *          and the result is written to stdout using UTF8 encoding.  If an error occurred, 
 *          the application exits with status 1 and the error is written to stderr.
 */

#include "stdafx.h"

using namespace std;

#define FILTER_INIT_OPTIONS \
	IFILTER_INIT_CANON_HYPHENS | \
	IFILTER_INIT_CANON_PARAGRAPHS | \
	IFILTER_INIT_CANON_SPACES | \
	IFILTER_INIT_APPLY_INDEX_ATTRIBUTES | \
	IFILTER_INIT_HARD_LINE_BREAKS
#define BUFLEN 65537
#define MAX_TEMP_PATH 4096

#ifdef UNICODE
#define tcout std::wcout
#define tcerr std::wcerr
#define tcin std::wcin
#else
#define tcout std::cout
#define tcerr std::cerr
#define tcin std::cin
#endif

static BOOL errorMessagePrinted = false;

BOOL CreateTempFile(TCHAR* szExtension, TCHAR* szTempDir, TCHAR* szTempFile) {
    BOOL bSuccess = false;

    // Determine the temporary directory (use the default windows temp directory if none is specified
    if (0 < _tcslen(szTempDir)) {
        wcsncpy_s(szTempFile, MAX_TEMP_PATH, szTempDir, MAX_TEMP_PATH);
    } else {
        if (0 == GetTempPath(MAX_TEMP_PATH, szTempFile)) {
            return bSuccess;
        }
    }

    // Create the temporary file that will contain binary stdin data
	UINT uRetVal = GetTempFileName(szTempFile, NULL, 0, szTempFile);
    if (0 != uRetVal) {
        DeleteFile(szTempFile);
        if (0 < _tcslen(szExtension) && '.' != szExtension[0]) {
            wcscat_s(szTempFile, MAX_TEMP_PATH, _T("."));
        }
        wcscat_s(szTempFile, MAX_TEMP_PATH, szExtension);
        HANDLE hTempFile = CreateFile(szTempFile, GENERIC_READ | GENERIC_WRITE, 0, NULL,  CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL); 
        if (NULL != hTempFile) {

            // Open stdin stream and write its data to the temporary file
            _setmode(_fileno(stdin), O_BINARY);
            FILE * input_file = _fdopen(_fileno(stdin), "rb");
            if (NULL != input_file) {
                long position = ftell(input_file);
                if (0 <= position) {
                    CHAR pbData[BUFLEN];
                    DWORD cbData, cbWritten;
                    while ((cbData = (DWORD)fread(pbData, sizeof(BYTE), BUFLEN, input_file)) > 0) {
                        if (!WriteFile(hTempFile, pbData, cbData, &cbWritten, NULL)) {
                            cbData = -1;
                            break;
                        } 
                    }
                    if (cbData >= 0) {
                        bSuccess = true;
                    }
                }
                ::fclose(input_file);
            }
            CloseHandle (hTempFile);
        }   
	} else {
		//DWORD lastError = GetLastError();
		//tcerr << "GetTempFileName failed with error " << lastError << endl;
	}
    return bSuccess;
}

HRESULT Analyze(wchar_t* szPath) {
    HRESULT hr = S_OK;

    // Load the IFilter associated with the specified file
    IFilter* pFilter;
    hr = LoadIFilter(szPath, NULL, (void**)&pFilter);
    if (SUCCEEDED(hr)) {

        // Initialize the IFilter
        DWORD dwFlags = 0;
        hr = pFilter->Init(FILTER_INIT_OPTIONS,0,NULL,&dwFlags);
        if (SUCCEEDED(hr)) {
            wchar_t szBuffer[BUFLEN];
            ULONG ulSize;
            STAT_CHUNK ps;
            while (SUCCEEDED(hr))
            {

                // Retrieve the next chunk in the document
                hr = pFilter->GetChunk(&ps);
                if ( (FILTER_E_EMBEDDING_UNAVAILABLE == hr) || (FILTER_E_LINK_UNAVAILABLE == hr) ) {
                    hr = S_OK;
                    continue;
                } else if (FILTER_E_END_OF_CHUNKS == hr) {
                    hr = S_OK;
                    break;
                }
                while(SUCCEEDED(hr)) {

                    // Retrieve the next block of text in the current chunk 
                    ulSize = BUFLEN;
                    hr = pFilter->GetText(&ulSize, szBuffer);
                    if ( (FILTER_E_NO_TEXT == hr) || (FILTER_E_NO_MORE_TEXT  == hr) ) {
                        hr = S_OK;
                        break;
                    }
                    if (SUCCEEDED(hr) && (0 < ulSize)) {
                        szBuffer[ulSize] = '\0';

                        // Convert to UTF8
                        unsigned int cbMultiByte = WideCharToMultiByte(CP_UTF8, NULL, szBuffer, -1, NULL, 0, NULL, NULL);
                        if (0 == cbMultiByte) {
                            hr = E_FAIL;
							tcerr << "WideCharToMultiByte#1 invocation failed" << endl;
							errorMessagePrinted = true;
                        } else {
                            char* pchMultiByte = new char[cbMultiByte];
                            if (NULL == pchMultiByte) {
                                hr = E_OUTOFMEMORY;
                            } else {
                                if (0 == WideCharToMultiByte(CP_UTF8, NULL, szBuffer, -1, pchMultiByte, cbMultiByte, NULL, NULL)) {
                                    hr = E_FAIL;
									tcerr << "WideCharToMultiByte#2 invocation failed" << endl;
									errorMessagePrinted = true;
                                } else {

                                    // Write the UTF8 text to stdout
                                    if (cbMultiByte > fwrite(pchMultiByte, 1, cbMultiByte, stdout)) {
                                        hr = E_FAIL;
										tcerr << "Unable to write converted bytes to output" << endl;
										errorMessagePrinted = true;
                                    }
                                }
                                delete[] pchMultiByte;
                            }
                        }
                    }
                } 
            }
		} else {
			tcerr << "IFilter initialization failed with HRESULT " << hr << endl;
			errorMessagePrinted = true;
		}
        pFilter->Release(); 
	} else {
		tcerr << "IFilter loading failed with HRESULT " << hr << endl;
		errorMessagePrinted = true;
	}
    return hr;
}

LONG WINAPI SE_UnhandledExceptionFilter(struct _EXCEPTION_POINTERS *pExInfo) {
    fprintf(stderr, "ERROR:  Unhandled Exception\nCode: 0x%8.8X\nFlags: %d\nAddress: 0x%8.8X\n",
        pExInfo->ExceptionRecord->ExceptionCode,
        pExInfo->ExceptionRecord->ExceptionFlags,
        pExInfo->ExceptionRecord->ExceptionAddress);
    exit(1);
	return EXCEPTION_CONTINUE_SEARCH;
}

int _tmain(int argc, _TCHAR* argv[])
{
    HRESULT hr = S_OK; 
    hr = CoInitialize(0);
	if (FAILED(hr)) {
		tcerr << "CoInitialize failed with HRESULT " << hr << endl;
		return 1;
	}

    SetErrorMode(SEM_NOGPFAULTERRORBOX);
    SetUnhandledExceptionFilter(SE_UnhandledExceptionFilter);

    if (SUCCEEDED(hr)) {
        wchar_t szTempFile[MAX_TEMP_PATH];
        if (!CreateTempFile(2 <= argc ? argv[1] : _T(""), 3 <= argc ? argv[2] : _T(""), szTempFile)) {
            hr = E_FAIL;
			if(3 <= argc) {
				tcerr << "Unable to create temp file in " << argv[2] << endl;
			} else {
				tcerr << "Unable to create temp file in default temp folder" << endl;
			}
			errorMessagePrinted = true;
        } else {
            hr = Analyze(szTempFile);
            DeleteFile(szTempFile);
        }
        CoUninitialize();
    }
    if (FAILED(hr)) {
		if(!errorMessagePrinted) {
			tcerr << "IFilter invocation failed with HRESULT " << hr << endl;
		}
        return 1;
    } else {
        return 0;
    }
}



