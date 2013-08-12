<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml"  encoding="UTF-8" indent="no" />

  <xsl:template match="/|*|@*|text()"><xsl:copy><xsl:apply-templates select="*|@*|text()" /></xsl:copy></xsl:template>

  <!-- convert 'libray' to 'extension' -->
  <xsl:template match="library"><extension><xsl:apply-templates /></extension></xsl:template>

  <!-- convert old 'param' format -->
  <xsl:template match="param[not(@name)]"><param name="{name}" type="{type}"><xsl:value-of select="hint" /></param></xsl:template>

  <!-- convert old 'return' format -->
  <xsl:template match="return[count(*) = 0 and count(@*) = 0 and text() != '']"><return type="{text()}" /></xsl:template>

  <!-- fix 'returnformat' nodes -->
  <xsl:template match="returnformat"><return type="{text()}" /></xsl:template>
</xsl:stylesheet>
