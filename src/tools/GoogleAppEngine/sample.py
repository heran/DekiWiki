#!/usr/bin/env python

import cgi
import wsgiref.handlers

from google.appengine.api import users
from google.appengine.ext import webapp

from dekiext import *

class MyExtension(DekiExt):

	# this method is required
	def title(self): return "My Extension"

	# the following methods are optional
	def description(self): return "My Description"
	def namespace(self): return "my"
	def copyright(self): return "My Copyright"
	def label(self): return "MyExt"

	# function to export
	@function("str", "return user greeting")
	@param("str", "name of user (default: \"stranger\")", True)
	def hello(self, name):
		return "Hi " + (name or "stranger!")

def main():
	application = webapp.WSGIApplication([('/', MyExtension)], debug=True)
	wsgiref.handlers.CGIHandler().run(application)

if __name__ == "__main__":
	main()