_shutter_server=192.168.1.6

function _shutter_get() {
    local uri=${1}

    curl -s -X GET http://${_shutter_server}/${uri} | 
        sed -e '/DOCTYPE/d' -e 's;<html>;;' -e 's;</html>;;' -e 's;\r;;'
}

function shutter_get_version() {
    _shutter_get version
}

function shutter_get_help() {
    _shutter_get help
}

function shutter_get_range() {
    _shutter_get range
}

function shutter_get_status() {
    _shutter_get status
}

function shutter_get_percent() {
    local min=14
    local max=226
    local current=$( shutter_get_range )

    echo $(( (( ${current} - ${min} ) * 100 ) / (${max} - ${min}) ))
}
