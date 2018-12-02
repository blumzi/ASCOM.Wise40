
# vim:se ai terse nows ts=4 sts=4 sw=4 expandtab:

telescope=${ascom_server}/telescope/0

function telescope_drivername() {
    GET ${telescope}/Name
}

function telescope_driverversion() {
    GET ${telescope}/DriverVersion
}

function telescope_driverinfo() {
    GET ${telescope}/DriverInfo
}

function telescope_targetrightascension() {
    local arg="${1}"

    if [ "${arg}" ]; then
        PUT ${telescope}/targetrightAscension "targetRightAscension=${arg}" 
    else
        GET ${telescope}/TargetRightAscension
    fi
}

function telescope_targetdeclination() {
    local arg="${1}"

    if [ "${arg}" ]; then
        PUT ${telescope}/targetdeclination "targetDeclination=${arg}" 
    else
        GET ${telescope}/TargetDeclination
    fi
}


function telescope_slewtocoordinatesasync() {
    local ra="${1}"
    local dec="${2}"

    PUT ${telescope}/slewToCoordinatesAsync "RightAscension=${ra}&Declibation=${dec}"
}

function telescope_slewing() {
    GET ${telescope}/Slewing
}

function telescope_tracking() {
    local arg="${1}"

    if [ "${arg}" ]; then
        PUT ${telescope}/Tracking "tracking=$(to_boolean ${arg})"
    else
        GET ${telescope}/Tracking
    fi
}


function telescope_rightascension() {
    GET ${telescope}/rightascension
}

function telescope_declination {
    GET ${telescope}/declination
}

function telescope_pulseguide() {
    local -A directions=( [north]=0 [south]=1 [east]=2 [west]=3 )
    local dir=${directions[${1,,}]}
    local millis="${2}"

    PUT ${telescope}/PulseGuide "Direction=${dir}&Duration=${millis}"
}

function telescope_ispulseguiding() {
    GET ${telescope}/IsPulseGuiding
}

function telescope_supportedactions() {
    GET ${telescope}/SupportedActions
}

function telescope_activities() {
    PUT ${telescope}/Action "Action=telescope:get-activities&Parameters="
}

function telescope_seconds_till_idle() {
    PUT ${telescope}/Action "Action=telescope:seconds-till-idle&Parameters="
}

function telescope_active() {
    local arg="${1}"

    if [ "${arg}" ]; then
        PUT ${telescope}/Action "Action=telescope:set-active&Parameters=$(to_boolean ${arg})"
    else
        PUT ${telescope}/Action "Action=telescope:get-active&Parameters="
    fi
}

function telescope_park() {
	PUT ${telescope}/Action "Action=telescope:shutdown&Parameters="
}

function telescope_unpark() {
	PUT ${telescope}/Unpark
}

function telescope_atpark() {
    GET ${telescope}/AtPark
}

function telescope_abortslew() {
    PUT ${telescope}/AbortSlew
}

function telescope_shutdown() {
    PUT ${telescope}/Action "Action=telescope:shutdown&Parameters="
}
