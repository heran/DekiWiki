# MindTouch Deki Extension for Google App Engine
# Copyright (C) 2006-2008 MindTouch, Inc.
# www.mindtouch.com  oss@mindtouch.com
#
# For community documentation and downloads visit www.opengarden.org;
# please review the licensing section.
#
# This library is free software; you can redistribute it and/or
# modify it under the terms of the GNU Lesser General Public
# License as published by the Free Software Foundation; either
# version 2.1 of the License, or (at your option) any later version.
# 
# This library is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
# Lesser General Public License for more details.
# 
# You should have received a copy of the GNU Lesser General Public
# License along with this library; if not, write to the Free Software
# Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
# http://www.gnu.org/copyleft/lesser.html

import cgi
import wsgiref.handlers

from google.appengine.api import users
from google.appengine.ext import webapp

# use @function for each method you wish to export in your extesion
def function(return_type, description=None, transform=None):
	def inner(f):
		if not hasattr(f, "deki_parameters"):
			f.deki_parameters = [ ]
		else:
			f.deki_parameters.reverse()
		assert len(f.deki_parameters) + 1 == f.func_code.co_argcount
		f.deki_return_type = return_type
		f.deki_description = description
		return f
	return inner

# use @param for each parameter on your exported extension function
def param(param_type, description=None, optional=False):
	def inner(f):
		if not hasattr(f, "deki_parameters"):
			f.deki_parameters = [ ]
		f.deki_parameters.append((param_type, description, optional));
		return f
	return inner

# derive from DekiExt to create an extension
class DekiExt(webapp.RequestHandler):

	# invoke extension method
	def post(self):
		import xmlrpclib
		p, m = xmlrpclib.loads(self.request.body)
		try:
			f = getattr(self, m)
			if f and callable(f):
				result = f(*p)
				xml = xmlrpclib.dumps((result,), methodresponse=1, allow_none=True)
			else:
				xml = xmlrpclib.dumps(xmlrpclib.Fault(-32400, 'system error: Cannot find or call %s' % m), methodresponse=1, allow_none=True)
		except Exception, e:
			xml = xmlrpclib.dumps(xmlrpclib.Fault(-32400, 'system error: %s' % e), methodresponse=1)
		self.response.headers['Content-Type'] = 'application/xml; charset=utf-8'
		self.response.out.write(xml)

	# get extension manifest
	def get(self):
		self.response.headers['Content-Type'] = 'application/xml; charset=utf-8'
		self.response.out.write("<extension>")
		
		# emit extension title
		self.response.out.write("<title>%s</title>" % (hasattr(self, "title") and self.title() or "Extension"))	
		
		# emit optional extension copyright
		if hasattr(self, "copyright"):
			self.response.out.write("<copyright>%s</copyright>" % self.copyright())
		
		# emit optional extension description
		if hasattr(self, "description"):
			self.response.out.write("<description>%s</description>" % self.description())
			
		# emit optional extension namespace
		if hasattr(self, "namespace"):
			self.response.out.write("<namespace>%s</namespace>" % self.namespace())
			
		# emit optional extension label
		if hasattr(self, "label"):
			self.response.out.write("<label>%s</label>" % self.label())
			
		# enumerate all functions
		for fname in dir(self):
			
			# check if function has the 'deki_return_type' field
			f = getattr(self, fname)
			if hasattr(f, "deki_return_type"):
				self.response.out.write("<function>")
				
				# emit function name
				self.response.out.write("<name>%s</name>" % f.__name__)
				
				# emit optional function description
				if f.deki_description != None:
					self.response.out.write("<description>%s</description>" % f.deki_description)
					
				# emit function uri
				path = self.request.path_url
				if path[len(path) - 1] != "/":
					path += "/"
				self.response.out.write("<uri protocol=\"xmlrpc\">%s</uri>" % (path + f.__name__ + ".rpc"))
				
				# emit function parameters
				i = 0
				for param in f.func_code.co_varnames:
					if param != "self":
						self.response.out.write("<param name=\"%s\" type=\"%s\"%s>%s</param>" % 
							(param, f.deki_parameters[i][0], f.deki_parameters[i][2] and " optional=\"true\"" or "", f.deki_parameters[i][1]))
						i += 1
				
				# emit function return type
				self.response.out.write("<return type=\"%s\"/>" % f.deki_return_type)
				self.response.out.write("</function>")
		self.response.out.write("</extension>")
