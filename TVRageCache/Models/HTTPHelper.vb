
Imports System.IO
Imports System.Net
Imports NLog

Namespace Models
	Public Enum LogLevels
		[Trace] = 0
		[Info] = 1
		[Debug] = 2
		[Warn] = 3
		[Error] = 4
	End Enum
	Public Class HTTPHelper
		Private _logger As NLog.Logger
		Private Property HTTPTimeOut As Integer
		Private Property HTTPAttempts As Integer
		Private Property DumpTextFiles As Boolean = False
		Public Shared Function Encode(ByVal str As String) As String
			Dim charClass = String.Format("0-9a-zA-Z{0}", Regex.Escape("-_.!~*'()"))
			Dim pattern = String.Format("[^{0}]", charClass)
			Dim evaluator As New MatchEvaluator(AddressOf EncodeEvaluator)

			' replace the encoded characters
			Return Regex.Replace(str, pattern, evaluator)
		End Function
		Private Shared Function EncodeEvaluator(ByVal match As Match) As String
			' Replace the " "s with "+"s
			If (match.Value = " ") Then
				Return "+"
			End If
			Return String.Format("%{0:X2}", Convert.ToInt32(match.Value.Chars(0)))
		End Function
		Public Shared Function Decode(ByVal str As String) As String
			Dim evaluator As New MatchEvaluator(AddressOf DecodeEvaluator)

			' Replace the "+"s with " "s
			str = str.Replace("+"c, " "c)

			' Replace the encoded characters
			Return Regex.Replace(str, "%[0-9a-zA-Z][0-9a-zA-Z]", evaluator)
		End Function

		Private Shared Function DecodeEvaluator(ByVal match As Match) As String
			Return "" + Convert.ToChar(Integer.Parse(match.Value.Substring(1), System.Globalization.NumberStyles.HexNumber))
		End Function
		Public Sub New(ByVal minLogLevel As LogLevels, Optional ByVal timeOut As Integer = 90000, Optional ByVal attempts As Integer = 3)
			HTTPTimeOut = timeOut
			HTTPAttempts = attempts

			' Step 1. Create configuration object 
			Dim config As New Config.LoggingConfiguration()

			Dim fileTarget As New Targets.FileTarget()
			config.AddTarget("file", fileTarget)

			' Step 3. Set target properties 
			'fileTarget.Layout = "${date:format=HH\:MM\:ss} ${logger} ${message}"
			fileTarget.FileName = "Logs\HTTPHelper_${date:format=yyyy.MM.dd-HH}.log"
			Dim rule1 As Config.LoggingRule
			Select Case minLogLevel
				Case LogLevels.Trace
					rule1 = New Config.LoggingRule("*", LogLevel.Trace, fileTarget)
				Case LogLevels.Info
					rule1 = New Config.LoggingRule("*", LogLevel.Info, fileTarget)
				Case LogLevels.Debug
					rule1 = New Config.LoggingRule("*", LogLevel.Debug, fileTarget)
				Case LogLevels.Warn
					rule1 = New Config.LoggingRule("*", LogLevel.Warn, fileTarget)
				Case LogLevels.Error
					rule1 = New Config.LoggingRule("*", LogLevel.Error, fileTarget)
			End Select

			' Step 4. Define rules
			'Dim rule1 As New Config.LoggingRule("*", mll, fileTarget)
			config.LoggingRules.Add(rule1)

			' Step 5. Activate the configuration
			LogManager.Configuration = config
			_logger = LogManager.GetLogger("HTTPHelper")
			If minLogLevel < LogLevels.Warn Then DumpTextFiles = True
		End Sub
		Public Function GetHTTPAsString(ByVal url As String) As HTTPResult
			Dim text = GetHTTP(url)
			Return text
		End Function
		Private Sub SaveTextFile(ByVal fileContent As String, ByVal fileName As String)
			If _DumpTextFiles Then
				Try
					_logger.Trace("Dumping " & fileName)
					Dim filewriter As New StreamWriter(fileName)
					filewriter.Write(fileContent)
					filewriter.Close()
				Catch ex As Exception
					_logger.Warn("Error when dumping file: " & ex.ToString)
				End Try
			End If
		End Sub
		Private Function GetFileName(Optional ByVal suffix As String = "") As String
			Dim filename = Format(Now, "yyyy-MM-dd_HHmmssff") & suffix & ".txt"
			filename = Path.GetInvalidFileNameChars().Aggregate(filename, Function(current, c) current.Replace(c.ToString(), String.Empty))
			Return Path.Combine(Environment.CurrentDirectory & "\Logs", filename)
		End Function
		Private Function GetHTTP(ByVal url As String) As HTTPResult
			Dim attempts = 1
			Dim hr As New HTTPResult
			Try
				Dim res As String
				Do
					_logger.Trace("Attempt " & attempts & " -- " & url)
					res = MakeRequest(url)
					If IsNothing(res) Then
						Dim sleepTime = 5000 * (attempts - 1) + 5000
						'error, let's give it a moment.
						_logger.Warn("Attempt {0}/{1} Failed, Retrying in {2} seconds - {3}", attempts, HTTPAttempts, sleepTime / 1000, url)
						attempts += 1
						If attempts < HTTPAttempts Then Threading.Thread.Sleep(sleepTime)
					End If
				Loop Until (attempts > HTTPAttempts) Or Not IsNothing(res)
				hr.Content = res
				hr.AttemptsMade = attempts
				If IsNothing(res) Or res.Length = 0 Or attempts > HTTPAttempts Then
					hr.GotResults = False
				Else
					hr.GotResults = True
				End If
				Return hr
			Catch ex As Exception
				_logger.Debug("After " & attempts & " server didn't pick up. Internet must be down (" & url & ").")
				Return New HTTPResult With {.AttemptsMade = attempts, .Content = "", .GotResults = False}
				'			Throw New Exception("The internet must be down, this ain't working " & url)
			End Try
		End Function
		Private Function MakeRequest(ByVal url As String) As String
			Try
				Dim wc As New XWebClient
				wc.Encoding = Encoding.UTF8
				'_logger.Trace(wc.Headers.AllKeys())
				Dim response = wc.DownloadString(url)
				SaveTextFile(Format(Now, "yyyy-MM-dd_HHmmssff") & " | " & url & vbCrLf & response, GetFileName("_OK"))
				If response.Length = 0 Then Return Nothing
				Return response

			Catch ex As Exception
				SaveTextFile(Format(Now, "yyyy-MM-dd_HHmmssff") & " | " & url & vbCrLf & ex.ToString, GetFileName("_ERR"))
				Return Nothing
			End Try


		End Function

	End Class
	Public Class HTTPResult
		Public Property Content As String
		Public Property AttemptsMade As String
		Public Property GotResults As Boolean
	End Class

	Class XWebClient
		Inherits WebClient
		Protected Overrides Function GetWebRequest(address As Uri) As WebRequest
			Dim request As HttpWebRequest = TryCast(MyBase.GetWebRequest(address), HttpWebRequest)
			request.AutomaticDecompression = DecompressionMethods.Deflate Or DecompressionMethods.GZip
			Return request
		End Function
		Public Sub New()
			MyBase.New()
			Randomize(Now.Ticks)
			Dim ua As String() = {"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_8_2) AppleWebKit/537.17 (KHTML, like Gecko) Chrome/24.0.1309.0 Safari/537.17" _
								 , "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.17 (KHTML, like Gecko) Chrome/24.0.1312.57 Safari/537.17" _
								 , "Mozilla/6.0 (Windows NT 6.2; WOW64; rv:16.0.1) Gecko/20121011 Firefox/16.0.1" _
								 , "Mozilla/5.0 (Mozilla/5.0 (Macintosh; Intel Mac OS X 10_6_8) AppleWebKit/537.13+ (KHTML, like Gecko) Version/5.1.7 Safari/534.57.2" _
								 , "Mozilla/5.0 (compatible; MSIE 10.6; Windows NT 6.1; Trident/5.0; InfoPath.2; SLCC1; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729; .NET CLR 2.0.50727) 3gpp-gba UNTRUSTED/1.0" _
								 , "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; WOW64; Trident/6.0)" _
								 , "Mozilla/5.0 (Windows; U; MSIE 9.0; Windows NT 9.0; en-US)" _
								 , "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:18.0) Gecko/20100101 Firefox/18.0" _
								 }
			Dim generator As System.Random = New System.Random()
			Dim selectedUA = generator.Next(0, UBound(ua))

			Headers.Add("Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
			Headers.Add("Accept-Charset: ISO-8859-1,utf-8;q=0.7,*;q=0.3")
			Headers.Add("Accept-Encoding: gzip,deflate")
			Headers.Add("Accept-Language: en-US,en;q=0.8")
			Headers.Add("User-Agent: " & ua(selectedUA))
		End Sub
	End Class
End Namespace
