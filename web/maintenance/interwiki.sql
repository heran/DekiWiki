-- Based more or less on the public interwiki map from MeatballWiki
-- Default interwiki prefixes...

REPLACE INTO /*$wgDBprefix*/interwiki (iw_prefix,iw_url,iw_local) VALUES
('google','http://www.google.com/search?q=$1',0),
('map','http://maps.google.com/maps?q=$1',0),
('wikipedia','http://en.wikipedia.org/wiki/$1',0);
