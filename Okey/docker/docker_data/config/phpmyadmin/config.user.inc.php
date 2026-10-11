<?php

// To deny access to all users by default, and then allow specific users
// See https://stackoverflow.com/a/44852146
$cfg['Servers'][1]['AllowDeny']['order'] = 'explicit';
$cfg['Servers'][1]['AllowDeny']['rules'] = [
    'allow readonly from all',
    'allow admin from all',
];
