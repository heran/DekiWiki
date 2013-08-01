package com.atlassian.confluence.extra.confmindtouch;

import com.atlassian.confluence.user.AuthenticatedUserThreadLocal;
import com.atlassian.user.User;
import com.atlassian.confluence.setup.settings.Settings;
import com.atlassian.confluence.spaces.SpaceManager;
import org.apache.log4j.Logger;
import java.util.Vector;
import com.atlassian.confluence.rpc.RemoteException;
import java.util.*;
import com.atlassian.confluence.labels.Label;
import org.apache.log4j.Logger;
import com.atlassian.confluence.spaces.Space;
import com.atlassian.confluence.labels.LabelManager;
import com.atlassian.spring.container.ContainerManager;
import com.atlassian.confluence.pages.TinyUrl;
import org.apache.commons.codec.binary.Base64;
import com.atlassian.confluence.setup.settings.Settings;
import com.atlassian.confluence.setup.settings.*;
import com.atlassian.confluence.spaces.SpaceDescription;
import org.springframework.transaction.PlatformTransactionManager;

public class ConfMindTouch implements ConfMindTouchPublic
{
	private SpaceManager spaceManager;
	private LabelManager labelManager;
	private SettingsManager settingsManager;
	// private PlatformTransactionManager platformTransactionManager;
	// private ConfMindTouchDelegator confMindTouchDelegator;

	private Logger log = Logger.getLogger(this.getClass());
	
	public String getTinyUrlForPageId(String pageid) throws RemoteException {
		
		String returnTinyURL = "";

		/*
		  use parseLong method of Long class to convert String into long primitive
		  data type. This is a static method.
		  Please note that this method can throw a NumberFormatException if the string
		  is not parsable to long.
		*/

		long l = Long.parseLong(pageid);		
	    returnTinyURL = makeSafeForUrl(Base64.encodeBase64(longToByteArray(l)));		
        Settings originalSettings = settingsManager.getGlobalSettings();
        String baseurl = settingsManager.getGlobalSettings().getBaseUrl();	    
	    return baseurl + "/x/" + returnTinyURL;		
	}
	
	public String getSpaceDescription(String spacekey) throws RemoteException 
	{
		String descReturn = "";		
		try {
			log.warn("getSpaceDescription: spacekey: " + spacekey);
			// Try to get the space specified by spacekey
			Space space = spaceManager.getSpace(spacekey);
			SpaceDescription desc = new SpaceDescription();
			
				if (space == null) 
				{
					throw new RemoteException();
				}
				else
				{   					
					desc = space.getDescription();
					descReturn = desc.getContent();
				}
		}
		catch (RemoteException re) {
			log.error(re.getLocalizedMessage());
			throw new RemoteException("getSpaceDescription: Failure getting space description for space " + spacekey, re);		
		}
		
		return descReturn;
	}
	
	
    public Vector getTeamLabels(String spacekey) throws RemoteException
    {
		Vector returnItem = new Vector();    	
		
		try {
			
			log.warn("getTeamLabelsForSpace: spacekey: " + spacekey);

			// Try to get the space specified by spacekey
			Space space = spaceManager.getSpace(spacekey);

			log.warn("getTeamLabelsForSpace: attempted to retrieve space - got space with key " + space.getKey());
			
				if (space == null) {
					throw new RemoteException();
				}
			
				// We want to return all team labels for this space			
				else {
						Vector returnObject = new Vector();							
						// Get any team labels from this space
						List teamLabelsForSpace = labelManager.getTeamLabelsForSpace(spacekey);				
						
						// Go through list, and if the one we want is in there, add this space to the list
						for (Iterator i = teamLabelsForSpace.iterator(); i.hasNext();) 
						{						
							Label tempTeamLabel = (Label) i.next();
							returnObject.add(tempTeamLabel);								
						}							
						returnItem = (Vector) returnObject;
						log.warn("getTeamLabelsForSpace: got this many team labels for space " + teamLabelsForSpace.size());
					}			
				} 
				
				catch (RemoteException re) 
				{					
					throw new RemoteException("getTeamLabelsForSpace: Failure executing API method for spacekey " + spacekey, re);
				}				
				
		return transformTeamLabelResult(returnItem);
    }
    
    private String makeSafeForUrl(byte[] bytes)
    {
        StringBuffer buf = new StringBuffer();
        boolean padding = true;
        for (int i = bytes.length - 1; i >= 0; i--)
        {
            byte b = bytes[i];
            if (b == '=' || b == 10)
                continue;
            if (padding && b == 'A')
                continue;

            padding = false;
            if (b == '/')
                buf.insert(0, '-');
            else if (b == '+')
                buf.insert(0, '_');
            else
                buf.insert(0, (char)b);

        }

        if(buf.length() > 0)
        {
            char lastChar = buf.charAt(buf.length() - 1);

            // CONF-9299 some email clients don't like URLs that end with a punctation
            if(lastChar == '-' || lastChar == '_')
                buf.append('/');
        }
        
        return buf.toString();
    }
    
    private static byte[] longToByteArray(long l) {
        byte[] retVal = new byte[8];

        for (int i = 0; i < 8; i++) {
            retVal[i] = (byte) l;
            l >>= 8;
        }

        return retVal;
    }
    
    
	/**
	 * @param permitted
	 *            List of SearchResult objects
	 * @return Vector of Hashtables
	 * */
	private Vector transformTeamLabelResult(Vector recent) {
				
		Vector pages = new Vector(recent.size());
				
		for (Iterator iter = recent.iterator(); iter.hasNext();) {
			Label result = (Label) iter.next();
			Hashtable pagehash = new Hashtable(1);			
			pagehash.put("Label", result.getName());
			pages.add(pagehash);
		}
		return pages;
	}
	
	
	public Space getSpace(String key) {
		return spaceManager.getSpace(key);		
	}
	
    
	public LabelManager getLabelManager() {
		//this.labelManager = (LabelManager) ContainerManager.getComponent("labelManager");
		return labelManager;
	}

	public void setLabelManager(LabelManager labelManager) {
		this.labelManager = labelManager;
	}
    
    public String helloWorld (){
    	return "Hello there";    	
    }
    
	public SpaceManager getSpaceManager() {
		//this.spaceManager = (SpaceManager) ContainerManager.getComponent("spaceManager");
		return spaceManager;
	}

	public void setSpaceManager(SpaceManager spaceManager) {
		this.spaceManager = spaceManager;
	}
	
	public SettingsManager getSettingsManager() {
		return settingsManager;
	}

	public void setSettingsManager(SettingsManager settingsManager) {
		this.settingsManager = settingsManager;
	}

	
	/*
	public PlatformTransactionManager getTransactionManager() {
		return platformTransactionManager;
	}

	public void setTransactionManager(
			PlatformTransactionManager transactionManager) {
		this.platformTransactionManager = transactionManager;
	}
		
	public void setConfMindTouchDelegator(ConfMindTouchDelegator Delegator)
	{
	        this.confMindTouchDelegator = confMindTouchDelegator;
	}
	*/
    
}



