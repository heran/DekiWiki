<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" />

  <!-- Extension Document -->
  <xsl:template match="extension">
    <html>
      <body>
        <p><xsl:if test="description"><xsl:value-of select="description"/></xsl:if> This script requires <strong><xsl:choose><xsl:when test="requires/@host"><xsl:value-of select="requires/@host"/></xsl:when><xsl:otherwise>MindTouch Core 1.8.3</xsl:otherwise></xsl:choose></strong> or later.</p>
        <xsl:apply-templates select="config"/>
        <p><strong>Functions:</strong></p>
        <ol>
          <xsl:for-each select="function[not(access) or access='public']">
            <xsl:sort select="name" />
            <li><a href="#f{position()}"><xsl:call-template name="qname" /></a></li>
          </xsl:for-each>
        </ol>
        <hr />
        <xsl:for-each select="function[not(access) or access='public']">
          <xsl:sort select="name" />
          <xsl:call-template name="function" />
        </xsl:for-each>
      </body>
    </html>
  </xsl:template>

  <!-- Config Section -->
  <xsl:template match="config">
    <xsl:if test="param">
      <p><strong>Configuration:</strong></p>
      <table cellspacing="0" cellpadding="1" border="1" style="width: 100%;">
        <tr style="background-image: none; vertical-align: top; background-color: #e1e1e1; text-align: left;">
          <td><strong>Config Key</strong></td>
          <td><strong>Type</strong></td>
          <td><strong>Description</strong></td>
        </tr>
        <xsl:call-template name="rows" />
      </table>
      <br />
    </xsl:if>
  </xsl:template>

  <!-- Function Entry -->
  <xsl:template name="function">
    <a id="f{position()}" />
    <h3><xsl:call-template name="signature" /></h3>
    <p>
      <xsl:value-of select="description"/>
      <xsl:if test="param">
        <p>
          <strong>Parameters:</strong>
        </p>
        <table cellspacing="0" cellpadding="1" border="1" style="width: 100%;">
          <tr style="background-image: none; vertical-align: top; background-color: #e1e1e1; text-align: left;">
            <td><strong>Name</strong></td>
            <td><strong>Type</strong></td>
            <td><strong>Description</strong></td>
          </tr>
          <xsl:call-template name="rows" />
        </table>
        <br />
      </xsl:if>
    </p>
    <hr />
  </xsl:template>

  <!-- Function Signature -->
  <xsl:template name="signature">
    <xsl:call-template name="qname"/>(<xsl:for-each select="param">
      <xsl:if test="position() &gt; 1">, </xsl:if>
      <xsl:value-of select="@name"/>
    </xsl:for-each>) : <xsl:choose>
      <xsl:when test="return[@type='application/x.dekiscript+xml']">xml</xsl:when>
      <xsl:when test="return/@type"><xsl:value-of select="return/@type"/></xsl:when>
      <xsl:when test="return/html">xml</xsl:when>
      <xsl:otherwise>any</xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- Qualifed Name -->
  <xsl:template name="qname">
    <xsl:choose>
      <xsl:when test="/extension/namespace[text()!='']"><xsl:value-of select="/extension/namespace"/>.<xsl:value-of select="name"/></xsl:when>
      <xsl:otherwise><xsl:value-of select="name"/></xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- Type Name -->
  <xsl:template name="type">
    <xsl:choose>
      <xsl:when test="@type"><xsl:value-of select="@type"/></xsl:when>
      <xsl:otherwise>any</xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- Table rows for config or parameter section -->
  <xsl:template name="rows">
    <xsl:for-each select="param">
      <tr>
        <td><xsl:value-of select="@name"/></td>
        <td>
          <xsl:choose>
            <xsl:when test="@type"><xsl:value-of select="@type"/></xsl:when>
            <xsl:otherwise>any</xsl:otherwise>
          </xsl:choose>
        </td>
        <td>
          <xsl:if test="@optional!='false' or @default">(optional) </xsl:if>
          <xsl:value-of select="text()"/>
          <xsl:if test="@default"> (default: <xsl:value-of select="@default"/>)</xsl:if>
        </td>
      </tr>
    </xsl:for-each>
  </xsl:template>
</xsl:stylesheet>