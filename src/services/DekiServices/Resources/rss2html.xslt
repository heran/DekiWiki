<?xml version="1.0" ?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
                xmlns:atom10="http://www.w3.org/2005/Atom"
                xmlns:atom03="http://purl.org/atom/ns#"  
                exclude-result-prefixes="atom10 atom03" >
  
  <xsl:output method="html" />
  <xsl:param name="format">table</xsl:param>
  <xsl:param name="max">-1</xsl:param>  
  
	<!-- match RSS 1.0 root node -->
	<xsl:template match="*[local-name()='RDF']">
    <xsl:if test="$format='table'">
      <div class="nowiki">
        <table class="feedtable" border="0" cellpadding="0" cellspacing="0">
          <thead>
            <xsl:for-each select="*[local-name()='channel']">
              <xsl:call-template name="feed_header"/>
            </xsl:for-each>
          </thead>
          <tbody>
            <xsl:for-each select="*[local-name()='item'][$max = -1 or position() &lt;= $max]">
              <xsl:call-template name="feed_item"/>
            </xsl:for-each>
          </tbody>
        </table>
      </div>
    </xsl:if>
    <xsl:if test="$format='list'">
      <div class="nowiki">
        <ul>
          <xsl:for-each select="*[local-name()='item'][$max = -1 or position() &lt;= $max]">
            <xsl:call-template name="feed_item"/>
          </xsl:for-each>
        </ul>
      </div>
    </xsl:if>
	</xsl:template>
	
	<!-- match RSS 0.91/2.0 root node -->
	<xsl:template match="/rss/channel">
    <xsl:if test="$format='table'" >
      <div class="nowiki">
        <table class="feedtable" border="0" cellpadding="0" cellspacing="0">
          <thead>
            <xsl:call-template name="feed_header"/>
          </thead>
          <tbody>
            <xsl:for-each select="*[local-name()='item'][$max = -1 or position() &lt;= $max]">
              <xsl:call-template name="feed_item"/>
            </xsl:for-each>
          </tbody>
        </table>
      </div>
    </xsl:if>
    <xsl:if test="$format='list'">
      <div class="nowiki">
        <ul>
          <xsl:for-each select="*[local-name()='item'][$max = -1 or position() &lt;= $max]">
            <xsl:call-template name="feed_item"/>
          </xsl:for-each>
        </ul>
      </div>
    </xsl:if>    
	</xsl:template>
	
	<!-- match ATOM 0.3/1.0 feed node -->
	<xsl:template match="atom03:feed|atom10:feed">
    <xsl:if test="$format='table'">
      <div class="nowiki">
        <table class="feedtable" border="0" cellpadding="0" cellspacing="0">
          <thead>
            <tr>
              <th>
                <xsl:choose>
                  <xsl:when test="*[local-name()='link']/@href != ''">
                    <a href="{*[local-name()='link']/@href}" target="blank">
                      <xsl:value-of select="*[local-name()='title']"/>
                    </a>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:value-of select="*[local-name()='title']"/>
                  </xsl:otherwise>
                </xsl:choose>
              </th>
            </tr>
          </thead>
          <tbody>
            <xsl:for-each select="*[local-name()='entry'][$max = -1 or position() &lt;= $max]">
              <xsl:element name="tr">

                <!-- check if we should use the EVEN or ODD row style -->
                <xsl:attribute name="class">
                  <xsl:if test="position() mod 2 = 0">feedroweven</xsl:if>
                  <xsl:if test="position() mod 2 = 1">feedrowodd</xsl:if>
                </xsl:attribute>

                <td>
                  <xsl:call-template name="atom_link"/>
                  <br/>
                  <!-- check if we should show the <content> or <summary> element -->
                  <xsl:choose>
                    <xsl:when test="*[local-name()='content'] != ''">
                      <xsl:value-of select="*[local-name()='content']" disable-output-escaping="yes" />
                    </xsl:when>
                    <xsl:otherwise>
                      <xsl:value-of select="*[local-name()='summary']" disable-output-escaping="yes" />
                    </xsl:otherwise>
                  </xsl:choose>
                </td>
              </xsl:element>
            </xsl:for-each>
          </tbody>
        </table>
      </div>
    </xsl:if>
    <xsl:if test="$format='list'">
      <div class="nowiki">
        <ul>
          <xsl:for-each select="*[local-name()='entry'][$max = -1 or position() &lt;= $max]">
            <li>
              <xsl:call-template name="atom_link"/>
            </li>
          </xsl:for-each>
        </ul>
      </div>
    </xsl:if>
	</xsl:template>

  <!-- template to show ATOM feed link -->
  <xsl:template name="atom_link">
    <xsl:choose>
      <xsl:when test="*[local-name()='link']/@href != ''">
        <xsl:if test="$format='table'" >
          <a href="{*[local-name()='link']/@href}" target="blank">
            <strong>
              <xsl:value-of select="*[local-name()='title']" disable-output-escaping="yes"/>
            </strong>
          </a>
        </xsl:if>
        <xsl:if test="$format='list'">
          <a href="{*[local-name()='link']/@href}" target="blank">
            <xsl:value-of select="*[local-name()='title']" disable-output-escaping="yes"/>
          </a>
        </xsl:if>
      </xsl:when>
      <xsl:otherwise>
        <xsl:if test="$format='table'" >
          <strong>
            <xsl:value-of select="*[local-name()='title']" disable-output-escaping="yes"/>
          </strong>
        </xsl:if>
        <xsl:if test="$format='list'">
          <xsl:value-of select="*[local-name()='title']" disable-output-escaping="yes"/>
        </xsl:if>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  
	<!-- template to show channel description -->
	<xsl:template name="feed_header">
    <xsl:if test="$format='table'">    
		  <tr>
			  <th>
          <xsl:choose>
            <xsl:when test="*[local-name()='link'] != ''">
              <a href="{*[local-name()='link']}" target="blank"><xsl:value-of select="*[local-name()='title']" disable-output-escaping="yes"/></a>
            </xsl:when>
            <xsl:otherwise>
              <xsl:value-of select="*[local-name()='title']" disable-output-escaping="yes"/>
            </xsl:otherwise>
          </xsl:choose>
		  	</th>
		  </tr>
    </xsl:if> 
	</xsl:template>
	
	<!-- template to show item description -->
	<xsl:template name="feed_item">
    <xsl:if test="$format='table'">
      <xsl:element name="tr">

        <!-- check if we should use the EVEN or ODD row style -->
        <xsl:attribute name="class">
          <xsl:if test="position() mod 2 = 0">feedroweven</xsl:if>
          <xsl:if test="position() mod 2 = 1">feedrowodd</xsl:if>
        </xsl:attribute>
        <td>
          <xsl:call-template name="feed_link"/>
          <br/>
          <xsl:value-of disable-output-escaping="yes" select="*[local-name()='description']"/>
        </td>
      </xsl:element>
    </xsl:if>
    <xsl:if test="$format='list'">
      <li>
        <xsl:call-template name="feed_link"/>
      </li>
    </xsl:if>
  </xsl:template>

<!-- template to show item link -->
  <xsl:template name="feed_link">
    <xsl:choose>
      <xsl:when test="*[local-name()='link'] != ''">
        <xsl:if test="$format='table'" >
          <a href="{*[local-name()='link']}" target="blank">
            <strong>
              <xsl:value-of select="*[local-name()='title']" disable-output-escaping="yes"/>
            </strong>
          </a>
        </xsl:if>
        <xsl:if test="$format='list'">
          <a href="{*[local-name()='link']}" target="blank">
            <xsl:value-of select="*[local-name()='title']" disable-output-escaping="yes"/>
          </a>
        </xsl:if>
      </xsl:when>
      <xsl:otherwise>
        <xsl:if test="$format='table'" >
          <strong>
            <xsl:value-of select="*[local-name()='title']" disable-output-escaping="yes"/>
          </strong>
        </xsl:if>
        <xsl:if test="$format='list'">
          <xsl:value-of select="*[local-name()='title']" disable-output-escaping="yes"/>
        </xsl:if>
      </xsl:otherwise>
    </xsl:choose>    
  </xsl:template>
</xsl:stylesheet>