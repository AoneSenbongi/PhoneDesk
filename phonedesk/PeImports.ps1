function Get-PeImports {
    param([byte[]]$Bytes)
    function U16([int]$At) { [BitConverter]::ToUInt16($Bytes,$At) }
    function U32([int]$At) { [BitConverter]::ToUInt32($Bytes,$At) }
    function ZString([int]$At) {
        $end=$At
        while ($end -lt $Bytes.Length -and $Bytes[$end] -ne 0) { $end++ }
        [Text.Encoding]::ASCII.GetString($Bytes,$At,$end-$At)
    }
    $pe=U32 0x3c
    if ((U32 $pe) -ne 0x4550) { throw 'Invalid PE image' }
    $optional=$pe+24
    if ((U16 $optional) -ne 0x20b) { throw 'Expected PE32+ x64 image' }
    $sectionCount=U16 ($pe+6)
    $sectionTable=$optional+(U16 ($pe+20))
    function RvaOffset([uint32]$Rva) {
        for ($s=0;$s -lt $sectionCount;$s++) {
            $base=$sectionTable+40*$s
            $va=U32 ($base+12)
            $size=[Math]::Max((U32 ($base+8)),(U32 ($base+16)))
            if ($Rva -ge $va -and $Rva -lt $va+$size) { return [int]((U32 ($base+20))+$Rva-$va) }
        }
        throw 'RVA outside PE sections'
    }
    $descriptor=RvaOffset (U32 ($optional+120))
    while ((U32 ($descriptor+12)) -ne 0) {
        $module=ZString (RvaOffset (U32 ($descriptor+12)))
        $lookup=U32 $descriptor
        if (!$lookup) { $lookup=U32 ($descriptor+16) }
        $thunk=RvaOffset $lookup
        while ($true) {
            $rva=[BitConverter]::ToUInt64($Bytes,$thunk)
            if (!$rva) { break }
            if ($rva -lt 0x100000000) {
                $nameOffset=(RvaOffset ([uint32]$rva))+2
                [pscustomobject]@{Module=$module;Name=(ZString $nameOffset);Offset=$nameOffset}
            }
            $thunk+=8
        }
        $descriptor+=20
    }
}
