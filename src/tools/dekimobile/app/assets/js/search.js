function highlight(e, url)
{
  e.style.background = "#d2d2d2 no-repeat top center";
  setTimeout("location.href=\'" + url + "\'", 200);
}
