local function includePath()
    local paths = {
        "C:/work/project/LuaBT/bt/?.lua",
    }
    for k,path in pairs(paths) do
        package.path = package.path .. ";" .. path
    end
end

includePath()
require("btHeader")

local btree = bt.BehaviourTree.new()
btree:load("C:/work/project/LuaBT/test/test.BT")