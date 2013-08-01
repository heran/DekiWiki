package com.atlassian.confluence.extra.confmindtouch;

import java.util.Vector;
import com.atlassian.confluence.rpc.RemoteException;

public interface ConfMindTouchPublic
{
    String helloWorld();
    Vector getTeamLabels(String spaceKey) throws RemoteException;
    String getTinyUrlForPageId(String pageid) throws RemoteException;
    String getSpaceDescription(String spaceKey) throws RemoteException;
}
