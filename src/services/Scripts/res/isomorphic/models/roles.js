/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
 * please review the licensing section.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 * http://www.gnu.org/copyleft/lesser.html
 */

isc.DataSource.create({
    ID: 'roles',
    dataFormat: 'xml',
    recordName: 'permissions',
    dataURL: '/@api/deki/site/roles',
    fields: [{
        title: 'ID',
        name: 'id',
        type: 'integer',
        valueXPath: 'role/@id',
        canSortClientOnly: true,
        primaryKey: true,
        canEdit: false
    }, {
        title: 'Role',
        name: 'role',
		type: 'text',
        valueXPath: 'role',
        canSortClientOnly: true,
        canEdit: false
    }, {
        title: 'Operations',
        name: 'operations',
		type: 'text',
        valueXPath: 'operations',
        canSortClientOnly: true,
        canEdit: false
    }],
    
    operationBindings: [{
        operationType: 'fetch',
        dataProtocol: 'getParams'
    }, {
        operationType: 'add',
        dataProtocol: 'clientCustom'
    }, {
        operationType: 'remove',
        dataProtocol: 'clientCustom'
    }, {
        operationType: 'update',
        dataProtocol: 'clientCustom'
    }],
    
    transformRequest: function(dsRequest){
        switch (dsRequest.operationType) {
            case 'fetch':
                return dsRequest.data;
            case 'add':
            case 'update':
            case 'remove':
                
                // NOTE (steveb): not supported
                break;
        }
        return dsRequest.data;
    },
});
