
# vim:se ai terse nows ts=4 sts=4 sw=4 expandtab:

telescope=${ascom_server}/telescope/0

function site_opmode() {
    local mode=${1^^}

    if [ "${mode}" ]; then
        case "${mode}" in
        LCO|WISE|ACP)
            ;;
        *)
            error "Unsupported site opmode \"${mode}\".  Must be either LCO, WISE or ACP!"
            return
            ;;
        esac
        PUT ${telescope}/Action "Action=site:set-opmode&Parameters=${mode}"
    else
        PUT ${telescope}/Action "Action=site:get-opmode&Parameters="
    fi
}

function site_altitude() {
	GET ${telescope}/Altitude
}

function site_elevation() {
	GET ${telescope}/SiteElevation
}

function site_latitude() {
    GET ${telescope}/SiteLatitude
}

function site_longitude() {
    GET ${telescope}/SiteLongitude
}

function site_siderealtime() {
    GET ${telescope}/SiderealTime
}

function site_utcdate() {
    GET ${telescope}/UTCDate
}
