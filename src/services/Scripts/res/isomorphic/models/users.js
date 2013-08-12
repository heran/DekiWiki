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
    ID: 'users',
    dataFormat: 'xml',
    recordName: 'user',
    dataURL: '/@api/deki/users',
    fields: [{
        title: 'ID',
        name: 'id',
        type: 'integer',
        valueXPath: '@id',
        primaryKey: true,
        canEdit: false
    }, {
        title: 'Gravatar',
        name: 'uri.gravatar',
        type: 'image',
        canSortClientOnly: true,
        canEdit: false
    }, {
        title: 'Username',
        name: 'username',
		type: 'text',
        required: true
    }, {
        title: 'Fullname',
        name: 'fullname',
		type: 'text'
    }, {
        title: 'EMail',
        name: 'email',
		type: 'text',
        required: true
    }, {
        title: 'Hash EMail',
        name: 'hash.email',
		type: 'text',
        canSortClientOnly: true,
        detail: true,
        canEdit: false
    }, {
        title: 'Created',
        name: 'date.created',
        type: 'datetime',
        canEdit: false
    }, {
        title: 'Status',
        name: 'status',
		type: 'text',
        required: true,
        valueMap: ['active', 'inactive']
    }, {
        title: 'Language',
        name: 'language',
		type: 'text',
        
        // TODO (steveb): read available languages from api
        
        canSortClientOnly: true
    }, {
        title: 'Timezone',
        name: 'timezone',
		type: 'text',
		valueMap: {
			'': '(site timezone)',
			'-11:00': 'Pacific/Midway (UTC−11)',
			'-10:00': 'Pacific/Honolulu (UTC−10)',
			'-09:30': 'Pacific/Marquesas (UTC−09:30)',
			'-09:00': 'America/Anchorage (UTC−09)',
			'-08:00': 'America/Los Angeles (UTC−08)',
			'-07:00': 'America/Denver (UTC−07)',
			'-06:00': 'America/Chicago (UTC−06)',
			'-05:00': 'America/New York (UTC−05)',
			'-04:30': 'America/Caracas (UTC−04:30)',
			'-04:00': 'Atlantic/Bermuda (UTC−04)',
			'-03:30': 'America/St Johns (UTC−03:30)',
			'-03:00': 'America/Sao Paulo (UTC−03)',
			'-02:00': 'America/Noronha (UTC−02)',
			'-01:00': 'Atlantic/Azores (UTC−01)',
			'00:00': 'Europe/London (UTC+00)',
			'+01:00': 'Europe/Paris (UTC+01)',
			'+02:00': 'Europe/Athens (UTC+02)',
			'+03:00': 'Europe/Moscow (UTC+03)',
			'+03:30': 'Asia/Tehran (UTC+03:30)',
			'+04:00': 'Asia/Dubai (UTC+04)',
			'+04:30': 'Asia/Kabul (UTC+04:30)',
			'+05:00': 'Indian/Maldives (UTC+05)',
			'+05:30': 'Asia/Colombo (UTC+05:30)',
			'+05:45': 'Asia/Kathmandu (UTC+05:45)',
			'+06:00': 'Asia/Karachi (UTC+06)',
			'+06:30': 'Asia/Rangoon (UTC+06:30)',
			'+07:00': 'Asia/Bangkok (UTC+07)',
			'+08:00': 'Asia/Shanghai (UTC+08)',
			'+08:45': 'Australia/Eucla (UTC+08:45)',
			'+09:00': 'Asia/Tokyo (UTC+09)',
			'+09:30': 'Australia/Adelaide (UTC+09:30)',
			'+10::00': 'Australia/Sydney (UTC+10)',
			'+10:30': 'Australia/Lord Howe (UTC+10:30)',
			'+11:00': 'Pacific/Guadalcanal (UTC+11)',
			'+11:30': 'Pacific/Norfolk (UTC+11:30)',
			'+12:00': 'Pacific/Fiji (UTC+12)',
			'+12:45': 'Pacific/Chatham (UTC+12:45)',
			'+13:00': 'Pacific/Enderbury (UTC+13)',
			'+14:00': 'Pacific/Kiritimati (UTC+14)'
		},        
        canSortClientOnly: true
    }, {
        title: 'Last Login',
        name: 'date.lastlogin',
        type: 'datetime',
        canEdit: false
    }, {
        title: 'User API',
        name: 'userApi',
        type: 'link',
        valueXPath: '@href',
        canSortClientOnly: true,
        detail: true,
        canEdit: false
    }, {
        title: 'Homepage ID',
        name: 'homepageId',
        type: 'integer',
        valueXPath: 'page.home/@id',
        canSortClientOnly: true,
        detail: true,
        canEdit: false
    }, {
        title: 'Homepage API',
        name: 'homepageApi',
        type: 'link',
        valueXPath: 'page.home/@href',
        canSortClientOnly: true,
        detail: true,
        canEdit: false
    }, {
        title: 'Homepage URI',
        name: 'homepageUri',
        type: 'link',
        valueXPath: 'page.home/uri.ui',
        canSortClientOnly: true,
        canEdit: false
    }, {
        title: 'Homepage Title',
        name: 'homepageTitle',
		type: 'text',
        valueXPath: 'page.home/title',
        canSortClientOnly: true,
        detail: true,
        canEdit: false
    }, {
        title: 'Homepage Path',
        name: 'homepagePath',
		type: 'text',
        valueXPath: 'page.home/path',
        canSortClientOnly: true,
        detail: true,
        canEdit: false
    }, {
        title: 'Auth-Service ID',
        name: 'authId',
        type: 'integer',
        required: true,
        valueXPath: 'service.authentication/@id',
        canSortClientOnly: true,
        detail: true
    }, {
        title: 'Auth-Service API',
        name: 'authApi',
        type: 'link',
        valueXPath: 'service.authentication/@href',
        canSortClientOnly: true,
        detail: true,
        canEdit: false
    }, {
        title: 'Permissions Mask',
        name: 'permissionsMask',
		type: 'text',
        valueXPath: 'permissions.user/operations',
        canSortClientOnly: true,
        detail: true,
        canEdit: false
    }, {
        title: 'Role',
        name: 'role',
		type: 'text',
        required: true,
        valueXPath: 'permissions.user/role',
        canSortClientOnly: true
    }, {
        title: 'Properties',
        name: 'properties',
        type: 'link',
        valueXPath: 'properties/@href',
        canSortClientOnly: true,
        detail: true,
        canEdit: false
    }],
    
    operationBindings: [{
        operationType: 'fetch',
        dataProtocol: 'getParams'
    }, {
        operationType: 'add',
        dataProtocol: 'postMessage'
    }, {
        operationType: 'remove',
        dataProtocol: 'clientCustom'
    }, {
        operationType: 'update',
        dataProtocol: 'postMessage'
    }],
    
    transformRequest: function(dsRequest){
        switch (dsRequest.operationType) {
            case 'fetch':
                var params = {
                    offset: dsRequest.startRow,
                    limit: dsRequest.endRow - dsRequest.startRow + 1
                };
                if (dsRequest.sortBy) {
                    params.sortby = dsRequest.sortBy;
                }
                return isc.addProperties({}, dsRequest.data, params);
            case 'add':
            case 'update':
                var result = '<user id="$id">' +
                '<nick>$nick</nick>' +
                '<username>$username</username>' +
                '<fullname>$fullname</fullname>' +
                '<email>$email</email>' +
                '<status>$status</status>' +
                '<language>$language</language>' +
                '<timezone>$timezone</timezone>' +
                '<service.authentication id="$authId" />' +
                '<permissions.user><role>$role</role></permissions.user>' +
                '</user>';
                result = result.replace(/\$[a-z]+/gi, function(match){
                    var value = (dsRequest.data[match.substr(1)] || '').toString();
                    return value.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;').replace('"', '&quot;');
                });
                return result;
            case 'remove':
                
                // NOTE (steveb): not supported
                break;
        }
        return dsRequest.data;
    },
    
    transformResponse: function(dsResponse, dsRequest, xmlData){
        dsResponse.startRow = dsRequest.startRow;
        dsResponse.totalRows = isc.XMLTools.selectNumber(xmlData, '@querycount');
        dsResponse.endRow = dsResponse.startRow + dsResponse.totalRows;
    }
});
