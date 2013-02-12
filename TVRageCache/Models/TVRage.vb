
Namespace Models
	Public Class TVRage

		Private _logger As NLog.Logger = NLog.LogManager.GetCurrentClassLogger()


		Public Function DoWork(ByVal operation As String, ByVal parameter As String) As ContentResult
			_logger.Warn("START CALL " & operation & "(" & parameter & ")")
			Dim sw As New Stopwatch
			sw.Start()
			Dim wc As New HTTPHelper(LogLevels.Warn, 50000, 7)
			Dim cr As New ContentResult
			Select Case operation
				Case "search"
					Dim result = wc.GetHTTPAsString("http://services.tvrage.com/feeds/search.php?show=" & HTTPHelper.Encode(parameter))
					If result.GotResults Then
						cr.ContentType = "application/xml"
						cr.Content = result.Content
					Else
						cr.ContentType = "text/plain"
						cr.Content = "504 timed out"
					End If
				Case "fullsearch"
					Dim result = wc.GetHTTPAsString("http://services.tvrage.com/feeds/full_search.php?show=" & HTTPHelper.Encode(parameter))
					If result.GotResults Then
						cr.ContentType = "application/xml"
						cr.Content = result.Content
					Else
						cr.ContentType = "text/plain"
						cr.Content = "504 timed out"
					End If
				Case "quickinfo"
					Dim result = wc.GetHTTPAsString("http://services.tvrage.com/tools/quickinfo.php?show=" & HTTPHelper.Encode(parameter))
					If result.GotResults Then
						cr.ContentType = "text/plain"
						cr.Content = result.Content
					Else
						cr.ContentType = "text/plain"
						cr.Content = "504 timed out"
					End If
				Case "showinfo"
					If IsNumeric(parameter) Then
						Dim result = wc.GetHTTPAsString("http://services.tvrage.com/feeds/showinfo.php?sid=" & HTTPHelper.Encode(parameter))
						If result.GotResults Then
							cr.ContentType = "application/xml"
							cr.Content = result.Content
						Else
							cr.ContentType = "text/plain"
							cr.Content = "504 timed out"
						End If
					Else
						cr.ContentType = "text/plain"
						cr.Content = "500 parameter error"
					End If
				Case "episodelist"
					If IsNumeric(parameter) Then
						Dim result = wc.GetHTTPAsString("http://services.tvrage.com/feeds/episode_list.php?sid=" & HTTPHelper.Encode(parameter))
						If result.GotResults Then
							cr.ContentType = "application/xml"
							cr.Content = result.Content
						Else
							cr.ContentType = "text/plain"
							cr.Content = "504 timed out"
						End If
					Else
						cr.ContentType = "text/plain"
						cr.Content = "500 parameter error"
					End If
				Case Else
					cr.ContentType = "text/plain"
					cr.Content = "500 operation error"
			End Select
			_logger.Warn("END   CALL ({0} ms.)", sw.ElapsedMilliseconds)
			sw.Stop()
			Return cr
		End Function
	End Class
End Namespace