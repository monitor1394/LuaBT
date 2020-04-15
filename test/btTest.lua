local function includePath()
    local paths = {
        "../LuaBT/?.lua",
        "../../XGame/XCommon/lua/?.lua",
    }
    for k,path in pairs(paths) do
        package.path = package.path .. ";" .. path
    end
end

includePath()
require("bt.btHeader")
require("ai.aiHeader")
require("lib.driver")
require("ai.api.server.APIAction")
require("ai.api.server.APICondition")

bt.ASSERT_DIR       = ""
bt.ASSERT_SUFFIX    = ".BT"

local btree = bt.BehaviourTree.new()
btree:load("test")
btree:start()

local function updateBT()
    bt.time = bt.time + bt.deltaTime
    bt.runLoopFunc()
    if btree then
        btree:update()
    end
    if bt.time > 20 then
        if btree then
            --btree:destroy()
            --btree = nil
        end
    end
end

xd.addTimer(0,bt.deltaTime * 1000,updateBT)