Imports TVRageCache.Models

Namespace Controllers
	Public Class HomeController
		Inherits System.Web.Mvc.Controller

		Private Shared _tvr As New TVRage
		Private Const CacheDuration As Integer = 12 * 60 * 60 ' 12 horas 
		Function Index(id) As ActionResult
			Return View()
		End Function

		<OutputCache(duration:=CacheDuration)> _
		Function Search(ByVal id As String) As ActionResult
			Return _tvr.DoWork("search", id)
		End Function

		<OutputCache(duration:=CacheDuration)> _
		Function FullSearch(ByVal id As String) As ActionResult
			Return _tvr.DoWork("fullsearch", id)
		End Function

		<OutputCache(duration:=CacheDuration)> _
		Function QuickInfo(ByVal id As String) As ActionResult
			Return _tvr.DoWork("quickinfo", id)
		End Function

		<OutputCache(duration:=CacheDuration)> _
		Function ShowInfo(ByVal id As String) As ActionResult
			Return _tvr.DoWork("showinfo", id)
		End Function

		<OutputCache(duration:=CacheDuration)> _
		Function EpisodeList(ByVal id As String) As ActionResult
			Return _tvr.DoWork("episodelist", id)
		End Function
	End Class


End Namespace