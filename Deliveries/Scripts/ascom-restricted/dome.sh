
# vim:se ai terse nows ts=4 sts=4 sw=4 expandtab:

dome=${ascom_server}/dome/0

function dome_get_drivername() {
    GET ${dome}/Name
}

function dome_driverversion() {
    GET ${dome}/DriverVersion
}

function dome_driverinfo() {
    GET ${dome}/DriverInfo
}

function dome_azimuth() {
    GET ${dome}/Azimuth 
}

function dome_slewtoazimuth() {
    local az=${1}

    PUT ${dome}/SlewToAzimuth "Azimuth=${az}"
}

function dome_supportedactions() {
    GET ${dome}/SupportedActions
}

function dome_status() {
    PUT ${dome}/Action "Action=digest&Parameters=" | jq
}

function dome_connected() {
    local arg="${1}"

    if [ "${arg}" ]; then
        PUT ${dome}/Connected "Connected=$(to_boolean ${arg})"
    else
        GET ${dome}/Connected
    fi
}

function dome_shutter_status() {
    local status=$( GET ${dome}/shutterStatus )
    local -A state=( [0]=open [1]=closed [2]=opening [3]=closing [4]=error )

    echo ${state[${status}]}
}

function dome_shutter_open() {
    PUT ${dome}/openShutter
}

function dome_shutter_close() {
    PUT ${dome}/closeShutter
}

function dome_projector() {
    local arg="${1}"

    if [ "${arg}" ]; then
        PUT ${dome}/Action "Action=dome:projector&Parameters=$(to_boolean ${arg})"
    else
        PUT ${dome}/Action "Action=dome:projector&Parameters="
    fi | jq
}

function dome_vent() {
    local arg="${1}"

    if [ "${arg}" ]; then
        PUT ${dome}/Action "Action=dome:vent&Parameters=$(to_boolean ${arg})"
    else
        PUT ${dome}/Action "Action=dome:vent&Parameters="
    fi | jq
}

function dome_get_atpark() {
    GET ${dome}/AtPark
}
