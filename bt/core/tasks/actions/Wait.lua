local Wait = bt.Class("Wait",bt.ActionTask)
bt.Wait = Wait

CompactStatus = {
    Failure = 0,
    Success = 1,
}

function Wait:ctor()
    bt.ActionTask.ctor(self)
    self.name = "Wait"
    self.waitTime = 1
    self.finishStatus = CompactStatus.Success
end

function Wait:init(jsonData)
    if jsonData.waitTime then
        self.waitTime = jsonData.waitTime._value
    end
    if jsonData.finishStatus ~= nil then
        self.finishStatus = CompactStatus[jsonData.finishStatus]
    end
end

function Wait:onUpdate()
    self:debug()
    if self:getElapsedTime() >= self.waitTime then
        if self.finishStatus == CompactStatus.Success then
            self:endAction(true)
        else
            self:endAction(false)
        end
    end
end

function Wait:info()
    return string.format("Wait %.2f/%.2f sec.",self:getElapsedTime(),self.waitTime)
end